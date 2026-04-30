using BlazorAppTest.Unit;
using FluentValidation.TestHelper;

namespace BlazorAppTest.Tests.Validators;

public class UnitValidatorTests
{
    private readonly StorageUnitValidator _storageValidator = new();
    private readonly TransportUnitValidator _transportValidator = new();

    [Fact]
    public void StorageUnit_Should_Have_Error_When_Kind_Is_Wrong()
    {
        // Arrange
        var unit = new StorageUnit { Kind = UnitKind.Production, Type = UnitType.Section}; // Намеренная ошибка

        // Act
        var result = _storageValidator.TestValidate(unit);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Kind)
            .WithErrorMessage("Для данного класса допустим только Kind = Storage");
    }

    [Fact]
    public void StorageUnit_Should_Have_Error_When_Type_Is_Not_For_Storage()
    {
        // Arrange
        var unit = new StorageUnit { Type = UnitType.Crane }; // Кран не может быть складом

        // Act
        var result = _storageValidator.TestValidate(unit);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void TransportUnit_Should_Be_Valid_When_Type_Is_Crane()
    {
        // Arrange
        var unit = new TransportUnit
        {
            Name = "Башенный кран",
            Type = UnitType.Crane,
            Code = "CRANE_01"
        };

        // Act
        var result = _transportValidator.TestValidate(unit);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UnitBase_Should_Fail_If_Parent_Is_Self()
    {
        // Arrange
        var id = Guid.NewGuid();
        var unit = new DepartmentUnit
        {
            Id = id,
            ParentId = id, // Ссылка на самого себя
            Type = UnitType.Section
        };
        var validator = new DepartmentUnitValidator();

        // Act
        var result = validator.TestValidate(unit);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParentId)
            .WithErrorMessage("Юнит не может быть родителем самого себя");
    }
}
