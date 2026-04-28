using BlazorAppTest.Domain;
using BlazorAppTest.Unit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorAppTest.Tests.Triggers;

public class DatabaseTriggerServiceTests
{
    [Fact]
    public async Task ValidateAsync_Should_Trigger_BaseType_Subscribers()
    {
        // Arrange
        // 1. Создаем минимальный контейнер для теста
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // 2. Достаем фабрику (она всегда есть в стандартном провайдере)
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        // 3. Передаем её в конструктор
        var service = new DatabaseTriggerService(scopeFactory);
        bool baseTriggerCalled = false;
        bool specificTriggerCalled = false;

        // Подписываемся на базовый тип
        service.BeforeSave<UnitBase>(async args =>
        {
            baseTriggerCalled = true;
            await Task.CompletedTask;
        });

        // Подписываемся на конкретный тип
        service.BeforeSave<StorageUnit>(async args =>
        {
            specificTriggerCalled = true;
            await Task.CompletedTask;
        });

        var storage = new StorageUnit { Id = Guid.NewGuid(), Name = "Test", Type = UnitType.Warehouse };

        // Act
        // Имитируем вызов из SaveChangesAsync
        await service.ValidateAsync(storage, EntityStateChangeEnum.Added, new List<PropertyChangeInfo>());

        // Assert
        baseTriggerCalled.Should().BeTrue("Триггер базового типа UnitBase должен был сработать");
        specificTriggerCalled.Should().BeTrue("Триггер конкретного типа StorageUnit должен был сработать");
    }

    [Fact]
    public async Task ValidateAsync_Should_Cancel_Execution_When_Trigger_Sets_Cancel()
    {
        // Arrange
        // 1. Создаем минимальный контейнер для теста
        var services = new ServiceCollection();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // 2. Достаем фабрику (она всегда есть в стандартном провайдере)
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        // 3. Передаем её в конструктор
        var service = new DatabaseTriggerService(scopeFactory);
        var errorMessage = "Отказано в доступе";

        service.BeforeSave<StorageUnit>(async args =>
        {
            args.Cancel = true;
            args.ErrorMessage = errorMessage;
            await Task.CompletedTask;
        });

        var storage = new StorageUnit { Id = Guid.NewGuid(), Name = "Test", Type = UnitType.Warehouse };

        // Act
        Func<Task> act = async () => await service.ValidateAsync(storage, EntityStateChangeEnum.Added, []);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>()
            .WithMessage(errorMessage);
    }

    [Fact]
    public async Task NotifyAsync_Should_Execute_After_Save_Subscribers()
    {
        // Arrange
        // 1. Создаем минимальный контейнер для теста
        var services = new ServiceCollection();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // 2. Достаем фабрику (она всегда есть в стандартном провайдере)
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        // 3. Передаем её в конструктор
        var service = new DatabaseTriggerService(scopeFactory);
        bool wasNotified = false;

        service.Subscribe<StorageUnit>(async args =>
        {
            wasNotified = true;
            await Task.CompletedTask;
        });

        var storage = new StorageUnit { Id = Guid.NewGuid(), Name = "Test", Type = UnitType.Warehouse };

        // Act
        await service.NotifyAsync(storage, EntityStateChangeEnum.Added, []);

        // Assert
        wasNotified.Should().BeTrue("Подписчик NotifyAsync должен был получить уведомление");
    }
}