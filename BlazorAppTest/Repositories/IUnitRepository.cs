using BlazorAppTest.Unit;

namespace BlazorAppTest.Repositories
{
    public interface IUnitRepository : IReferenceRepository<UnitBase>
    {
        /// <summary>
        /// Получает все юниты с предварительной загрузкой дочерних элементов (Eager Loading).
        /// </summary>
        Task<List<UnitBase>> GetAllWithChildrenAsync();

        /// <summary>
        /// Перемещает юнит к новому родителю. 
        /// Валидация на циклы произойдет автоматически в DatabaseTriggerService.
        /// </summary>
        /// <param name="unitId">ID перемещаемого юнита</param>
        /// <param name="newParentId">ID нового родителя (null, если узел становится корневым)</param>
        Task MoveAsync(Guid unitId, Guid? newParentId);

        /// <summary>
        /// Получает только корневые элементы (у которых ParentId == null) со всеми вложенными детьми.
        /// Полезно для построения дерева в UI.
        /// </summary>
        Task<List<UnitBase>> GetRootNodesAsync();
    }
}