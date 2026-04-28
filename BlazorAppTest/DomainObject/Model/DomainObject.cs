namespace BlazorAppTest.DomainObject.Model;

/// <summary>
/// Основной класс (GUID по умолчанию)
/// </summary>
public abstract class DomainObject : DomainObjectBase<Guid>
{
    protected DomainObject()
    {
        Id = Guid.NewGuid();
    }
}