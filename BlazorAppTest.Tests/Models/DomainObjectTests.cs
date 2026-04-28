using FluentAssertions;

namespace BlazorAppTest.Tests.Models;

public class DomainObjectTests
{
    // Тестовый класс-наследник, так как DomainObject абстрактный
    private class TestEntity : Domain.DomainObject { }

    [Fact]
    public void Id_Should_Be_Generated_Upon_Initialization()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void CreatedAt_Should_Be_Set_Automatically()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}