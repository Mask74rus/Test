using BlazorAppTest.Unit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace BlazorAppTest.Repositories;

public class UnitRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
    : ReferenceRepository<UnitBase>(contextFactory), IUnitRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory1 = contextFactory;

    // 1. Получение всех юнитов с детьми (плоский список, но со связями)
    public async Task<List<UnitBase>> GetAllWithChildrenAsync()
    {
        await using var context = await _contextFactory1.CreateDbContextAsync();
        return await context.Units
            .Include(x => x.Children)
            .ToListAsync();
    }

    // 2. Получение только корней (для начала построения дерева)
    public async Task<List<UnitBase>> GetRootNodesAsync()
    {
        await using var context = await _contextFactory1.CreateDbContextAsync();
        return await context.Units
            .Include(x => x.Children)
            .Where(x => x.ParentId == null)
            .ToListAsync();
    }

    // 3. Логика перемещения
    public async Task MoveAsync(Guid unitId, Guid? newParentId)
    {
        await using var context = await _contextFactory1.CreateDbContextAsync();
        var unit = await context.Units.FindAsync(unitId);

        if (unit != null)
        {
            unit.ParentId = newParentId;
            // Здесь сработает наш DatabaseTriggerService и проверит на циклы
            await context.SaveChangesAsync();
        }
    }

    // Переопределяем стандартный GetAll, чтобы он по умолчанию грузил дерево
    public override async Task<List<UnitBase>> GetAllAsync()
    {
        return await GetAllWithChildrenAsync();
    }
}