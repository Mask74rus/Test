using BlazorAppTest.Unit;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BlazorAppTest.Tests.Models;

public class UnitHierarchyTests
{
    // Фикс для времени: создаем дату чуть-чуть в прошлом для валидатора
    private readonly DateTime _pastTime = DateTime.UtcNow.AddSeconds(-10);

    [Theory]
    [InlineData(UnitType.Workstation, true)]
    [InlineData( UnitType.Warehouse, false)] // Склад не может быть отделом
    public void DepartmentUnit_Validation_Rules(UnitType type, bool shouldBeValid)
    {
        // Arrange
        var unit = new DepartmentUnit
        {
            Name = "Тест",
            Type = type,
            CreatedAt = _pastTime
        };
        var validator = new DepartmentUnitValidator();

        // Act
        var result = validator.TestValidate(unit);

        // Assert
        if (shouldBeValid)
            result.ShouldNotHaveAnyValidationErrors();
        else
            result.ShouldHaveValidationErrorFor(x => x.Type);

        unit.Kind.Should().Be(UnitKind.Department);
    }

    [Fact]
    public void StorageUnit_Should_Have_Specific_Properties()
    {
        // Arrange & Act
        var unit = new StorageUnit
        {
            Name = "Авто-Склад",
            Type = UnitType.Warehouse,
            IsAutomaticArchiving = true,
            CreatedAt = _pastTime
        };

        // Assert
        unit.Kind.Should().Be(UnitKind.Storage);
        unit.IsAutomaticArchiving.Should().BeTrue();
    }

    [Fact]
    public void PositionUnit_Should_Validate_OrderNo()
    {
        // Arrange
        var unit = new PositionUnit
        {
            Name = "Позиция",
            Type = UnitType.Workstation,
            OrderNo = -1, // Ошибка: должно быть >= 0
            CreatedAt = _pastTime
        };
        var validator = new PositionUnitValidator();

        // Act
        var result = validator.TestValidate(unit);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderNo);
    }

    [Theory]
    [InlineData(UnitType.Crane, true)]
    [InlineData(UnitType.Vehicle, true)]
    [InlineData(UnitType.Workshop, false)] // Цех — это не транспорт
    public void TransportUnit_Type_Constraints(UnitType type, bool isValid)
    {
        // Arrange
        var unit = new TransportUnit
        {
            Name = "Транспорт",
            Type = type,
            CreatedAt = _pastTime
        };
        var validator = new TransportUnitValidator();

        // Act
        var result = validator.TestValidate(unit);

        // Assert
        if (isValid)
            result.ShouldNotHaveAnyValidationErrors();
        else
            result.ShouldHaveValidationErrorFor(x => x.Type);
    }
}