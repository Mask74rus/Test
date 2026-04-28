using BlazorAppTest.Domain;
using BlazorAppTest.Unit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorAppTest.Tests.Triggers;

public class DatabaseTriggerServiceTests
{
    // Вспомогательный метод для создания пустого контекста для тестов
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ValidateAsync_Should_Trigger_BaseType_Subscribers()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var service = new DatabaseTriggerService(scopeFactory);

        bool baseTriggerCalled = false;
        bool specificTriggerCalled = false;

        service.BeforeSave<UnitBase>(async args =>
        {
            baseTriggerCalled = true;
            await Task.CompletedTask;
        });

        service.BeforeSave<StorageUnit>(async args =>
        {
            specificTriggerCalled = true;
            await Task.CompletedTask;
        });

        var storage = new StorageUnit { Id = Guid.NewGuid(), Name = "Test", Type = UnitType.Warehouse };
        using var context = CreateContext(); // Создаем контекст для теста

        // Act
        // Передаем context четвертым аргументом
        await service.ValidateAsync(storage, EntityStateChangeEnum.Added, new List<PropertyChangeInfo>(), context);

        // Assert
        baseTriggerCalled.Should().BeTrue();
        specificTriggerCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_Should_Cancel_Execution_When_Trigger_Sets_Cancel()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var service = new DatabaseTriggerService(scopeFactory);
        var errorMessage = "Отказано в доступе";

        service.BeforeSave<StorageUnit>(async args =>
        {
            args.Cancel = true;
            args.ErrorMessage = errorMessage;
            await Task.CompletedTask;
        });

        var storage = new StorageUnit { Id = Guid.NewGuid(), Name = "Test", Type = UnitType.Warehouse };
        using var context = CreateContext();

        // Act
        // Передаем context
        Func<Task> act = async () => await service.ValidateAsync(storage, EntityStateChangeEnum.Added, [], context);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>()
            .WithMessage(errorMessage);
    }

    [Fact]
    public async Task NotifyAsync_Should_Execute_After_Save_Subscribers()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var service = new DatabaseTriggerService(scopeFactory);
        bool wasNotified = false;

        service.AfterSave<StorageUnit>(async args =>
        {
            wasNotified = true;
            await Task.CompletedTask;
        });

        var storage = new StorageUnit { Id = Guid.NewGuid(), Name = "Test", Type = UnitType.Warehouse };

        // Act
        // Теперь передаем дополнительные параметры: [], "TestUser", DateTime.UtcNow
        await service.NotifyAsync(storage, EntityStateChangeEnum.Added, [], "TestUser", DateTime.UtcNow);

        // Assert
        wasNotified.Should().BeTrue("Подписчик AfterSave должен был получить уведомление с метаданными");
    }
}