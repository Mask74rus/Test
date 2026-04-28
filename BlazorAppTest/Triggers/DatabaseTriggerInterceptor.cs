using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BlazorAppTest.Domain;

public class DatabaseTriggerInterceptor(DatabaseTriggerService triggerService) : SaveChangesInterceptor
{
    // 1. ВАЛИДАЦИЯ ПЕРЕД СОХРАНЕНИЕМ
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context == null) return result;

        var entries = GetChanges(eventData.Context);

        foreach (var item in entries)
        {
            // Передаем текущий контекст для быстрой работы триггеров
            await triggerService.ValidateAsync(item.Entity, item.State, item.Changes, eventData.Context);
        }

        return result;
    }

    // 2. УВЕДОМЛЕНИЯ ПОСЛЕ УСПЕШНОГО СОХРАНЕНИЯ
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        if (eventData.Context == null) return result;

        // После сохранения Added/Modified стали Unchanged. Собираем их снова для уведомлений.
        var entries = GetChanges(eventData.Context, isPostSave: true);

        foreach (var item in entries)
        {
            await triggerService.NotifyAsync(item.Entity, item.State, item.Changes);
        }

        return result;
    }

    private List<ChangeEntryModel> GetChanges(DbContext context, bool isPostSave = false)
    {
        return context.ChangeTracker.Entries()
            .Where(e => isPostSave
                ? e.State == EntityState.Unchanged // Те, что успешно сохранились
                : e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => new ChangeEntryModel
            {
                Entity = e.Entity,
                State = MapState(isPostSave ? EntityState.Unchanged : e.State), // Тут можно доработать логику определения стейта для уведомлений
                Changes = !isPostSave && e.State == EntityState.Modified
                    ? e.Properties.Where(p => p.IsModified).Select(p => new PropertyChangeInfo
                    {
                        PropertyName = p.Metadata.Name,
                        OriginalValue = p.OriginalValue,
                        CurrentValue = p.CurrentValue
                    }).ToList() : []
            }).ToList();
    }

    private EntityStateChangeEnum MapState(EntityState state) => state switch
    {
        EntityState.Added => EntityStateChangeEnum.Added,
        EntityState.Deleted => EntityStateChangeEnum.Deleted,
        _ => EntityStateChangeEnum.Modified
    };

    private class ChangeEntryModel
    {
        public object Entity { get; init; } = null!;
        public EntityStateChangeEnum State { get; init; }
        public List<PropertyChangeInfo> Changes { get; init; } = [];
    }
}