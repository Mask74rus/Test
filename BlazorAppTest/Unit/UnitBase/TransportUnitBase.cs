namespace BlazorAppTest.Unit;

// Транспортная единица
public abstract class TransportUnitBase : UnitBase
{
    protected TransportUnitBase() => Kind = UnitKind.Transport;
}