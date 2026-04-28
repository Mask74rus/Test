using FluentValidation;

namespace BlazorAppTest.Unit;

// Валидатор для Производства
public class ProductionUnitValidator : UnitBaseValidator<ProductionUnit>
{
    public ProductionUnitValidator()
    {
        RuleFor(x => x.Kind)
            .Equal(UnitKind.Production)
            .WithMessage("Для данного класса допустим только Kind = Production");

        RuleFor(x => x.Type)
            .Must(t => t == UnitType.Workshop || t == UnitType.Section || t == UnitType.Line || t == UnitType.MachineTool)
            .WithMessage("Выбранный тип не соответствует производственной логике");
    }
}