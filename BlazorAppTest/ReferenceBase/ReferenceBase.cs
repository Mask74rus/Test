

namespace BlazorAppTest.Domain;

/// <summary>
/// Базовый класс для всех справочников
/// </summary>
public abstract class ReferenceBase : DomainObject.Model.DomainObject
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Code { get; set; }
}