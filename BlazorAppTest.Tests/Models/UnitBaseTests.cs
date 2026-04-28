using BlazorAppTest.Unit;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BlazorAppTest.Tests.Models;

public class UnitBaseTests
{
    [Fact]
    public void UnitBase_Should_Require_Type_On_Initialization()
    {
        // Теперь мы обязаны указать Type при создании
        var unit = new DepartmentUnit
        {
            Name = "Отдел ИТ",
            Type = UnitType.Other // Обязательное поле
        };

        unit.Type.Should().Be(UnitType.Other);
        unit.Kind.Should().Be(UnitKind.Department);
    }
}

public class UnitValidatorTests
{
    private readonly StorageUnitValidator _storageValidator = new();
    private readonly TransportUnitValidator _transportValidator = new();

    [Fact]
    public void StorageUnit_Should_Have_Error_When_Type_Is_Not_For_Storage()
    {
        // Arrange
        var unit = new StorageUnit
        {
            Name = "Тест",
            Type = UnitType.Crane // Type указан, но он не подходит для склада по бизнес-логике
        };

        // Act
        TestValidationResult<StorageUnit>? result = _storageValidator.TestValidate(unit);
        
        // Assert
        // 1. Проверяем наличие ошибки для свойства
        ITestValidationWith? validationErrors = result.ShouldHaveValidationErrorFor(x => x.Type);

        // 2. Проверяем текст ошибки через обычный FluentAssertions
        validationErrors.Where(e => e.ErrorMessage.Contains("не применим к складской единице"))
            .Should().NotBeEmpty("Сообщение об ошибке должно содержать текст о неприменимости типа");
    }

    [Fact]
    public void TransportUnit_Should_Be_Valid_When_Type_Is_Correct()
    {
        // Arrange
        var unit = new TransportUnit
        {
            Name = "Погрузчик",
            Type = UnitType.Vehicle, // Обязательный и корректный тип
            Code = "TRUCK_01"
        };

        // Act
        TestValidationResult<TransportUnit>? result = _transportValidator.TestValidate(unit);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Unit_Should_Fail_If_Parent_Is_Self()
    {
        // Arrange
        var id = Guid.NewGuid();
        var unit = new PositionUnit
        {
            Id = id,
            Name = "Позиция",
            Type = UnitType.Workstation, // Обязательное поле
            ParentId = id
        };
        var validator = new PositionUnitValidator();

        // Act
        TestValidationResult<PositionUnit>? result = validator.TestValidate(unit);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParentId)
              .WithErrorMessage("Юнит не может быть родителем самого себя");
    }
}