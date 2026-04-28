using FluentValidation;

namespace BlazorAppTest.Unit;

public class TransportUnitValidator : UnitBaseValidator<TransportUnit>
{
    public TransportUnitValidator()
    {
        // Проверка категории
        RuleFor(x => x.Kind)
            .Equal(UnitKind.Transport)
            .WithMessage("Для данного класса допустим только Kind = Transport");

        // Ограничение типов (кран, транспортное средство, конвейер)
        RuleFor(x => x.Type)
            .Must(t => t == UnitType.Crane || t == UnitType.Vehicle || t == UnitType.Conveyor)
            .WithMessage(x => $"Тип '{x.Type}' не относится к транспортному оборудованию");

        // Если в будущем добавите в TransportUnit поле MaxLoadCapacity:
        // RuleFor(x => x.MaxLoadCapacity).GreaterThan(0).WithMessage("Грузоподъемность должна быть больше нуля");
    }
}