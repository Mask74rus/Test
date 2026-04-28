using FluentValidation;

namespace BlazorAppTest.Unit;

public class DepartmentUnitValidator : UnitBaseValidator<DepartmentUnit>
{
    public DepartmentUnitValidator()
    {
        // Проверка категории
        RuleFor(x => x.Kind)
            .Equal(UnitKind.Department)
            .WithMessage("Для данного класса допустим только Kind = Department");

        // Ограничение типов (отдел, рабочее место и т.д.)
        RuleFor(x => x.Type)
            .Must(t => t == UnitType.Other || t == UnitType.Workstation)
            .WithMessage(x => $"Тип '{x.Type}' обычно не используется для административных подразделений");
    }
}