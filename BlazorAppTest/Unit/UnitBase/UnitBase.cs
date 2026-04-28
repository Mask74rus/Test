using BlazorAppTest.Domain;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorAppTest.Unit;

public abstract class UnitBase : ReferenceBase
{
    public UnitKind Kind { get; init; }
    
    public required UnitType Type { get; set; }

    public Guid? ParentId { get; set; }

    [ForeignKey("ParentId")]
    public virtual UnitBase? Parent { get; set; }

    // Список дочерних элементов
    public virtual ICollection<UnitBase> Children { get; set; } = new List<UnitBase>();
}