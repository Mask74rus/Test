using BlazorAppTest.DomainObject.Interface;
using BlazorAppTest.Unit;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace BlazorAppTest.Domain;

public static class DbTriggersConfiguration
{
    public static void RegisterDomainTriggers(this IServiceProvider services)
    {
        var triggerService = services.GetRequiredService<DatabaseTriggerService>();

        // 1. Общая валидация через FluentValidation
        triggerService.BeforeSave<IDomainObjectHasKey<Guid>>(async args =>
        {
            // Нам все еще нужен scope для получения валидаторов из DI
            using IServiceScope scope = triggerService.ScopeFactory.CreateScope();

            Type entityType = args.Entity.GetType();
            Type validatorType = typeof(IValidator<>).MakeGenericType(entityType);

            if (scope.ServiceProvider.GetService(validatorType) is IValidator validator)
            {
                var context = new ValidationContext<object>(args.Entity);
                ValidationResult result = await validator.ValidateAsync(context);

                if (!result.IsValid)
                {
                    args.Cancel = true;
                    args.ErrorMessage = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));
                }
            }
        });

        // 2. Специфичная проверка иерархии (UnitBase) через ПРЯМОЙ контекст
        triggerService.BeforeSave<UnitBase>(async args =>
        {
            // Проверка удаления: есть ли дети?
            if (args.State == EntityStateChangeEnum.Deleted)
            {
                // Используем args.Context напрямую из интерцептора
                bool hasChildren = await args.Context.Set<UnitBase>()
                    .AnyAsync(u => u.ParentId == args.Entity.Id);

                if (hasChildren)
                {
                    args.Cancel = true;
                    args.ErrorMessage = "Нельзя удалить объект, у которого есть дочерние элементы.";
                    return;
                }
            }

            // Проверка циклической зависимости
            bool isParentChanged = args.State == EntityStateChangeEnum.Added ||
                                   args.Changes.Any(c => c.PropertyName == nameof(UnitBase.ParentId));

            if (isParentChanged && args.Entity.ParentId.HasValue)
            {
                Guid? currentId = args.Entity.ParentId;
                Guid targetId = args.Entity.Id;
                int maxDepth = 50;
                int depth = 0;

                while (currentId.HasValue && depth < maxDepth)
                {
                    if (currentId.Value == targetId)
                    {
                        args.Cancel = true;
                        args.ErrorMessage = "Циклическая зависимость: выбранный родитель является дочерним элементом текущего объекта.";
                        return;
                    }

                    // Поднимаемся по дереву, используя тот же контекст транзакции
                    currentId = await args.Context.Set<UnitBase>()
                        .AsNoTracking()
                        .Where(u => u.Id == currentId)
                        .Select(u => u.ParentId)
                        .FirstOrDefaultAsync();

                    depth++;
                }
            }
        });
    }
}