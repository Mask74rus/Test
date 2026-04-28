using BlazorAppTest.Interfaces;
using BlazorAppTest.Unit;
using Microsoft.EntityFrameworkCore;

namespace BlazorAppTest.Domain;

public class SoftDeleteValidationTrigger : IBeforeSaveTrigger<UnitBase>
{
    public async Task HandleAsync(EntityCancelEventArgs<UnitBase> args)
    {
        if (args.State == EntityStateChangeEnum.SoftDeleted)
        {
            bool hasActiveChildren = await args.Context.Set<UnitBase>()
                .AnyAsync(u => u.ParentId == args.Entity.Id && u.DeletedAt == null);

            if (hasActiveChildren)
            {
                args.Cancel = true;
                args.ErrorMessage = "Нельзя архивировать объект с активными дочерними элементами.";
            }
        }
    }
}