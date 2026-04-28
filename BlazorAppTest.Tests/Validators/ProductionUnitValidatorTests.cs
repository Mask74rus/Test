using BlazorAppTest.Unit;
using FluentValidation.TestHelper;

namespace BlazorAppTest.Tests.Validators;

public class ProductionUnitValidatorTests
{
    private readonly ProductionUnitValidator _validator;

    public ProductionUnitValidatorTests()
    {
        // Если вы убрали ContextFactory из конструктора:
        _validator = new ProductionUnitValidator();

        // Если же оставили, передайте Mock или реальную фабрику из тестов:
        // _validator = new ProductionUnitValidator(null!); 
    }

    [Fact]
    public void Should_Be_Valid_When_Production_Logic_Is_Met()
    {
        // Arrange
        var unit = new ProductionUnit
        {
            Name = "Цех сборки",
            Kind = UnitKind.Production,
            Type = UnitType.Workshop, // Разрешенный тип
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        TestValidationResult<ProductionUnit>? result = _validator.TestValidate(unit);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Kind_Is_Incorrect()
    {
        // Arrange
        var unit = new ProductionUnit
        {
            Name = "Ошибка категории",
            Kind = UnitKind.Transport, // Неверный Kind
            Type = UnitType.Workshop
        };

        // Act
        TestValidationResult<ProductionUnit>? result = _validator.TestValidate(unit);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Kind)
            .WithErrorMessage("Для данного класса допустим только Kind = Production");
    }

    [Theory]
    [InlineData(UnitType.Crane)]      // Кран - не производство
    [InlineData(UnitType.Warehouse)]  // Склад - не производство
    [InlineData(UnitType.Vehicle)]    // Транспорт - не производство
    public void Should_Have_Error_When_Type_Is_Not_Production(UnitType invalidType)
    {
        // Arrange
        var unit = new ProductionUnit
        {
            Name = "Тест типа",
            Kind = UnitKind.Production,
            Type = invalidType
        };

        // Act
        TestValidationResult<ProductionUnit>? result = _validator.TestValidate(unit);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage("Выбранный тип не соответствует производственной логике");
    }
}