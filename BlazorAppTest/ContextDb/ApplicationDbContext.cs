using BlazorAppTest.Domain;
using BlazorAppTest.DomainObject.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlazorAppTest;

public partial class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    // Свойство для ленивого получения сервиса из DI-контейнера
    private DatabaseTriggerService TriggerService => this.GetService<DatabaseTriggerService>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Указываем схему по умолчанию для всех таблиц этого контекста
        modelBuilder.HasDefaultSchema("test");

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IDomainObjectHasKey<Guid>).IsAssignableFrom(entityType.ClrType))
            {
                PropertyBuilder property = modelBuilder.Entity(entityType.ClrType)
                    .Property("Id")
                    .ValueGeneratedNever(); // Прямое указание базе не генерировать ключ  
            }
        }

        modelBuilder.RegisterUnitEntities();

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => new
            {
                Entry = e,
                Entity = e.Entity,
                State = MapState(e.State),
                // Вычисляем изменения полей
                Changes = e.State == EntityState.Modified
                    ? e.Properties
                        .Where(p => p.IsModified)
                        .Select(p => new PropertyChangeInfo
                        {
                            PropertyName = p.Metadata.Name,
                            OriginalValue = p.OriginalValue,
                            CurrentValue = p.CurrentValue
                        }).ToList()
                    : []
            })
            .ToList();

        // 1. Асинхронная валидация (Before)
        foreach (var item in entries)
            await TriggerService.ValidateAsync(item.Entity, item.State, item.Changes);

        int result = await base.SaveChangesAsync(ct);

        // 2. Асинхронные уведомления (After)
        foreach (var item in entries)
            await TriggerService.NotifyAsync(item.Entity, item.State, item.Changes);

        return result;
    }

    private EntityStateChangeEnum MapState(EntityState state) => state switch
    {
        EntityState.Added => EntityStateChangeEnum.Added,
        EntityState.Deleted => EntityStateChangeEnum.Deleted,
        _ => EntityStateChangeEnum.Modified
    };
}