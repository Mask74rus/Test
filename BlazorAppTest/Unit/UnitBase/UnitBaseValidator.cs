using BlazorAppTest.Domain;
using FluentValidation;

namespace BlazorAppTest.Unit;

public class UnitBaseValidator<T> : ReferenceBaseValidator<T> where T : UnitBase
{
    public UnitBaseValidator()
    {
        RuleFor(x => x.Kind)
            .IsInEnum().WithMessage("Указана недопустимая категория (Kind)");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Указан недопустимый тип (Type)");

        // Проверка иерархии: объект не может быть своим собственным родителем
        RuleFor(x => x.ParentId)
            .NotEqual(x => x.Id)
            .When(x => x.ParentId.HasValue)
            .WithMessage("Юнит не может быть родителем самого себя");
    }
}