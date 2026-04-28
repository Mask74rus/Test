using BlazorAppTest.Domain;
using FluentAssertions;

namespace BlazorAppTest.Tests.Models;

public class ReferenceBaseTests
{
    private class TestReference : ReferenceBase { }

    [Fact]
    public void ReferenceBase_ShouldInheritDomainObjectProps()
    {
        // Arrange & Act
        var reference = new TestReference();

        // Assert
        // ID генерируется при создании объекта
        reference.Id.Should().NotBe(Guid.Empty);

        reference.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ReferenceBase_ShouldStoreBasicFields()
    {
        var reference = new TestReference
        {
            Name = "Тест",
            Code = "TEST_CODE",
            Description = "Описание"
        };

        reference.Name.Should().Be("Тест");
        reference.Code.Should().Be("TEST_CODE");
        reference.Description.Should().Be("Описание");
    }
}