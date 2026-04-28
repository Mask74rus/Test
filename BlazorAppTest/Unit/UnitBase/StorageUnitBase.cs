namespace BlazorAppTest.Unit;

// Складская единица
public abstract class StorageUnitBase : UnitBase
{
    protected StorageUnitBase() => Kind = UnitKind.Storage;

    public virtual bool IsAutomaticArchiving { get; set; } = false;
}