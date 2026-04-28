using FluentValidation;

namespace BlazorAppTest.Unit;

// Валидатор для Позиций
public class PositionUnitValidator : UnitBaseValidator<PositionUnit>
{
    public PositionUnitValidator()
    {
        RuleFor(x => x.Kind)
            .Equal(UnitKind.Position)
            .WithMessage("Для данного класса допустим только Kind = Position");

        RuleFor(x => x.OrderNo)
            .GreaterThanOrEqualTo(0);
    }
}