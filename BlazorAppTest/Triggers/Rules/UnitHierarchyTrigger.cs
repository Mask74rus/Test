using BlazorAppTest.Interfaces;
using BlazorAppTest.Unit;
using Microsoft.EntityFrameworkCore;

namespace BlazorAppTest.Domain;

public class UnitHierarchyTrigger : IBeforeSaveTrigger<UnitBase>
{
    public async Task HandleAsync(EntityCancelEventArgs<UnitBase> args)
    {
        // 1. Проверка физического удаления
        if (args.State == EntityStateChangeEnum.Deleted)
        {
            if (await args.Context.Set<UnitBase>().AnyAsync(u => u.ParentId == args.Entity.Id))
            {
                args.Cancel = true;
                args.ErrorMessage = "Нельзя удалить объект, у которого есть дочерние элементы.";
                return;
            }
        }

        // 2. Проверка циклов
        bool isParentChanged = args.State == EntityStateChangeEnum.Added ||
                               args.Changes.Any(c => c.PropertyName == nameof(UnitBase.ParentId));

        if (isParentChanged && args.Entity.ParentId.HasValue)
        {
            Guid? currentId = args.Entity.ParentId;
            Guid targetId = args.Entity.Id;
            int depth = 0;

            while (currentId.HasValue && depth < 50)
            {
                if (currentId.Value == targetId)
                {
                    args.Cancel = true;
                    args.ErrorMessage = "Циклическая зависимость: выбранный родитель является дочерним элементом.";
                    return;
                }
                currentId = await args.Context.Set<UnitBase>()
                    .AsNoTracking()
                    .Where(u => u.Id == currentId)
                    .Select(u => u.ParentId)
                    .FirstOrDefaultAsync();
                depth++;
            }
        }
    }
}