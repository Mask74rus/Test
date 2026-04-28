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

        // Добавляем async
        triggerService.BeforeSave<IDomainObjectHasKey<Guid>>(async args =>
        {
            using IServiceScope scope = triggerService.ScopeFactory.CreateScope();

            Type entityType = args.Entity.GetType();
            Type validatorType = typeof(IValidator<>).MakeGenericType(entityType);

            if (scope.ServiceProvider.GetService(validatorType) is IValidator validator)
            {
                var context = new ValidationContext<object>(args.Entity);
                // Используем асинхронную валидацию
                ValidationResult result = await validator.ValidateAsync(context);

                if (!result.IsValid)
                {
                    args.Cancel = true;
                    args.ErrorMessage = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));
                }
            }
            // Если ValidateAsync не использовался, нужно было бы дописать await Task.CompletedTask;
        });

        // 2. Специфичная проверка иерархии (только для UnitBase)
        triggerService.BeforeSave<UnitBase>(async args =>
        {
            // Проверяем, если это создание или если ParentId был изменен
            bool isParentChanged = args.State == EntityStateChangeEnum.Added ||
                                   args.Changes.Any(c => c.PropertyName == nameof(UnitBase.ParentId));

            if (args.State == EntityStateChangeEnum.Deleted)
            {
                using var scope = triggerService.ScopeFactory.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                await using var context = await factory.CreateDbContextAsync();

                // Проверяем, есть ли у этого объекта дети
                bool hasChildren = await context.Units
                    .AnyAsync(u => u.ParentId == args.Entity.Id);

                if (hasChildren)
                {
                    args.Cancel = true;
                    args.ErrorMessage = "Нельзя удалить объект, у которого есть дочерние элементы. Сначала удалите или переместите их.";
                }
            }

            if (isParentChanged && args.Entity.ParentId.HasValue)
            {
                using IServiceScope scope = triggerService.ScopeFactory.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                await using ApplicationDbContext context = await factory.CreateDbContextAsync();

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

                    // Поднимаемся выше по дереву через базу
                    currentId = await context.Units
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