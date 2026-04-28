using BlazorAppTest.DomainObject.Interface;
using BlazorAppTest.Interfaces;
using BlazorAppTest.Unit;

namespace BlazorAppTest.Domain;

public static class DatabaseTriggersConfiguration
{
    public static void RegisterDomainTriggers(this IServiceProvider services)
    {
        var triggerService = services.GetRequiredService<DatabaseTriggerService>();

        // --- ЯВНАЯ РЕГИСТРАЦИЯ ТРИГГЕРОВ ---

        // 1. Валидация (BeforeSave)
        // FluentValidationTrigger будет работать для всех объектов с Guid-ключом
        triggerService.Register<IDomainObjectHasKey<Guid>, FluentValidationTrigger>();

        // Специфичные правила для иерархии UnitBase
        triggerService.Register<UnitBase, UnitHierarchyTrigger>();
        triggerService.Register<UnitBase, SoftDeleteValidationTrigger>();

        // 2. Уведомления (AfterSave)
        // Пример триггера для интеграции (мягкое удаление)
        triggerService.Register<ISoftDeletable, UnitArchiveNotificationTrigger>();
    }
}