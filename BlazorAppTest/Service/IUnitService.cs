using BlazorAppTest.Unit;

namespace BlazorAppTest.Service;

public interface IUnitService : IReferenceService<UnitBase>
{
    Task<List<UnitBase>> GetAllWithChildrenAsync();

    /// <summary>
    /// Получение всех юнитов в виде плоского списка, но с подгруженными детьми
    /// </summary>
    Task<List<UnitBase>> GetTreeAsync();

    /// <summary>
    /// Получение только корневых элементов (у которых ParentId == null)
    /// </summary>
    Task<List<UnitBase>> GetRootNodesAsync();

    /// <summary>
    /// Перемещение юнита к новому родителю
    /// </summary>
    /// <param name="unitId">ID перемещаемого юнита</param>
    /// <param name="newParentId">ID нового родителя (null, если узел станет корневым)</param>
    Task MoveAsync(Guid unitId, Guid? newParentId);
}