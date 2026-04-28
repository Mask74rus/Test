namespace BlazorAppTest.Unit;

// Производственная единица
public abstract class ProductionUnitBase : UnitBase
{
    protected ProductionUnitBase() => Kind = UnitKind.Production;
}