namespace BlazorAppTest.Unit;

// Подразделение / Отдел
public abstract class DepartmentUnitBase : UnitBase
{
    protected DepartmentUnitBase() => Kind = UnitKind.Department;
}