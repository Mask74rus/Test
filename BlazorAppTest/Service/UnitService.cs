using BlazorAppTest.Unit;
using Microsoft.EntityFrameworkCore;

namespace BlazorAppTest.Service;

public class UnitService(IDbContextFactory<ApplicationDbContext> contextFactory)
    : ReferenceService<UnitBase>(contextFactory), IUnitService
{

    public async Task<List<UnitBase>> GetAllWithChildrenAsync() // Должен быть public!
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        return await context.Units.Include(x => x.Children).ToListAsync();
    }

    public async Task<List<UnitBase>> GetTreeAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        return await context.Units
            .Include(x => x.Children)
            .Where(x => x.ParentId == null)
            .ToListAsync();
    }

    public Task<List<UnitBase>> GetRootNodesAsync()
    {
        throw new NotImplementedException();
    }

    public async Task MoveAsync(Guid unitId, Guid? newParentId)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var unit = await context.Units.FindAsync(unitId);

        if (unit != null)
        {
            unit.ParentId = newParentId;
            // Здесь можно добавить проверку: не является ли новый родитель потомком?
            await context.SaveChangesAsync();
        }
    }
}