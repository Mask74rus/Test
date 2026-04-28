namespace BlazorAppTest.Unit;

public class PositionUnitBase : UnitBase
{
    protected PositionUnitBase() => Kind = UnitKind.Position;

    public virtual bool IsMultiple { get; set; }

    public virtual int OrderNo { get; set; }
}