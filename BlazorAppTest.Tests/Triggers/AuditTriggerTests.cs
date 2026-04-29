using BlazorAppTest.Audit;
using BlazorAppTest.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BlazorAppTest.Tests.Triggers;

public class AuditTriggerTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    [Fact]
    public async Task HandleAsync_WhenStateIsAdded_ShouldNotContainOldValueInJson()
    {
        // Arrange
        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
            .ReturnsAsync(() => new ApplicationDbContext(_options));

        var trigger = new AuditTrigger(factoryMock.Object);

        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        var changes = new List<PropertyChangeInfo>
        {
            new() { PropertyName = "Name", OriginalValue = null, CurrentValue = "Test" }
        };

        var args = new EntityChangedEventArgs<Domain.DomainObject>(
            entity,
            EntityStateChangeEnum.Added,
            changes,
            "Admin",
            DateTime.UtcNow);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        await using var context = new ApplicationDbContext(_options);
        AuditLog? log = await context.Set<AuditLog>().FirstOrDefaultAsync();

        log.Should().NotBeNull();
        log.Action.Should().Be("Added");
        log.ChangesJson.Should().NotContain("OldValue"); // Проверка, что OldValue удален
        log.ChangesJson.Should().Contain("NewValue");
    }

    [Fact]
    public async Task HandleAsync_WhenStateIsModified_ShouldContainOldAndNewValues()
    {
        // Arrange
        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
            .ReturnsAsync(() => new ApplicationDbContext(_options));

        var trigger = new AuditTrigger(factoryMock.Object);

        var entity = new TestEntity { Id = Guid.NewGuid()  };
        var changes = new List<PropertyChangeInfo>
        {
            new() { PropertyName = "Name", OriginalValue = "OldName", CurrentValue = "NewName" }
        };

        var args = new EntityChangedEventArgs<Domain.DomainObject>(
            entity,
            EntityStateChangeEnum.Modified,
            changes,
            "Admin",
            DateTime.UtcNow);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        await using var context = new ApplicationDbContext(_options);
        AuditLog? log = await context.Set<AuditLog>().FirstOrDefaultAsync();

        log.Should().NotBeNull();
        log.ChangesJson.Should().Contain("OldValue");
        log.ChangesJson.Should().Contain("NewValue");
        log.ChangesJson.Should().Contain("OldName");
        log.ChangesJson.Should().Contain("NewName");
    }
}

// Вспомогательный класс для тестов
public class TestEntity : Domain.DomainObject { public string Name { get; set; } }