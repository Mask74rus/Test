using FluentValidation;

namespace BlazorAppTest.Unit;

// Валидатор для Складов
public class StorageUnitValidator : UnitBaseValidator<StorageUnit>
{
    public StorageUnitValidator()
    {
        RuleFor(x => x.Kind)
            .Equal(UnitKind.Storage)
            .WithMessage("Для данного класса допустим только Kind = Storage");

        // Можно также ограничить допустимые типы оборудования для склада
        RuleFor(x => x.Type)
            .Must(t => t == UnitType.Warehouse || t == UnitType.Zone || t == UnitType.Rack || t == UnitType.Shelf || t == UnitType.Cell)
            .WithMessage(x => $"Тип {x.Type} не применим к складской единице");
    }
}