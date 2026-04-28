using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Concurrent;

namespace BlazorAppTest.Domain;

public class DatabaseTriggerInterceptor(
    DatabaseTriggerService triggerService,
    IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    // Хранилище для передачи данных между Saving и Saved в рамках одного потока/контекста
    private readonly ConcurrentDictionary<Guid, List<ChangeEntryModel>> _capturedChanges = new();

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context == null) return result;

        string? userName = await GetUserNameAsync();
        List<ChangeEntryModel> entries = CaptureChanges(eventData.Context, userName);

        // Кэшируем изменения по уникальному ID экземпляра контекста
        _capturedChanges[eventData.Context.ContextId.InstanceId] = entries;

        foreach (ChangeEntryModel item in entries)
        {
            await triggerService.ValidateAsync(item.Entity, item.State, item.Changes, eventData.Context);
        }

        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        if (eventData.Context != null && _capturedChanges.TryRemove(eventData.Context.ContextId.InstanceId, out List<ChangeEntryModel>? entries))
        {
            foreach (ChangeEntryModel item in entries)
            {
                // Передаем захваченные метаданные в уведомления
                await triggerService.NotifyAsync(item.Entity, item.State, item.Changes, item.ChangedBy, item.ChangedAt);
            }
        }
        return result;
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken ct = default)
    {
        if (eventData.Context != null)
            _capturedChanges.TryRemove(eventData.Context.ContextId.InstanceId, out _);

        return base.SaveChangesFailedAsync(eventData, ct);
    }

    private List<ChangeEntryModel> CaptureChanges(DbContext context, string? userName)
    {
        return context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => new ChangeEntryModel
            {
                Entity = e.Entity,
                State = MapState(e.State),
                ChangedBy = userName,
                ChangedAt = DateTime.UtcNow,
                Changes = e.State == EntityState.Modified
                    ? e.Properties.Where(p => p.IsModified).Select(p => new PropertyChangeInfo
                    {
                        PropertyName = p.Metadata.Name,
                        OriginalValue = p.OriginalValue,
                        CurrentValue = p.CurrentValue
                    }).ToList()
                    : []
            }).ToList();
    }

    private async Task<string?> GetUserNameAsync()
    {
        try
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            var authStateProvider = scope.ServiceProvider.GetService<AuthenticationStateProvider>();
            if (authStateProvider != null)
            {
                AuthenticationState state = await authStateProvider.GetAuthenticationStateAsync();
                return state.User.Identity?.Name;
            }
        }
        catch { }
        return "System";
    }

    private EntityStateChangeEnum MapState(EntityState state) => state switch
    {
        EntityState.Added => EntityStateChangeEnum.Added,
        EntityState.Deleted => EntityStateChangeEnum.Deleted,
        _ => EntityStateChangeEnum.Modified
    };

    // Внутренняя модель для кэширования
    private class ChangeEntryModel
    {
        public object Entity { get; init; } = null!;
        public EntityStateChangeEnum State { get; init; }
        public List<PropertyChangeInfo> Changes { get; init; } = [];
        public string? ChangedBy { get; init; }
        public DateTime ChangedAt { get; init; }
    }
}