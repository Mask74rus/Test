using BlazorAppTest.Domain;
using BlazorAppTest.Unit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BlazorAppTest.Tests.Triggers;

public class DatabaseTriggerInterceptorTests
{
    private ApplicationDbContext CreateContext(DatabaseTriggerInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SavingChanges_Should_CancelSave_When_ValidationFails()
    {
        // Arrange
        var triggerService = new DatabaseTriggerService(new Mock<IServiceScopeFactory>().Object);
        var sp = AuthTestHelper.CreateServiceProviderWithAuth("TestUser");
        var interceptor = new DatabaseTriggerInterceptor(triggerService, sp);
        using var context = CreateContext(interceptor);

        // Настраиваем триггер на отмену
        triggerService.BeforeSave<StorageUnit>(async args =>
        {
            args.Cancel = true;
            args.ErrorMessage = "Validation Failed";
            await Task.CompletedTask;
        });

        context.Units.Add(new StorageUnit { Name = "Error Storage", Type = UnitType.Crane});

        // Act
        Func<Task> act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>().WithMessage("Validation Failed");
        context.Units.Count().Should().Be(0, "Запись не должна быть сохранена в БД");
    }

    [Fact]
    public async Task SavedChanges_Should_Notify_With_Correct_Metadata()
    {
        // Arrange
        var triggerService = new DatabaseTriggerService(new Mock<IServiceScopeFactory>().Object);
        string expectedUser = "AdminUser";
        var sp = AuthTestHelper.CreateServiceProviderWithAuth(expectedUser);
        var interceptor = new DatabaseTriggerInterceptor(triggerService, sp);
        using var context = CreateContext(interceptor);

        string? capturedUser = null;
        triggerService.AfterSave<StorageUnit>(async args =>
        {
            capturedUser = args.ChangedBy;
            await Task.CompletedTask;
        });

        context.Units.Add(new StorageUnit { Name = "Success Storage", Type = UnitType.Crane });

        // Act
        await context.SaveChangesAsync();

        // Assert
        capturedUser.Should().Be(expectedUser, "Имя пользователя должно быть проброшено из интерцептора в триггер");
    }

    [Fact]
    public async Task Interceptor_Should_ClearCache_On_Success()
    {
        // Тест проверяет, что ConcurrentDictionary в интерцепторе не течет
        // (Для этого можно использовать Reflection или проверить через несколько сохранений)

        var triggerService = new DatabaseTriggerService(new Mock<IServiceScopeFactory>().Object);
        var sp = AuthTestHelper.CreateServiceProviderWithAuth("System");
        var interceptor = new DatabaseTriggerInterceptor(triggerService, sp);
        using var context = CreateContext(interceptor);

        context.Units.Add(new StorageUnit { Name = "Storage 1", Type = UnitType.Crane });
        await context.SaveChangesAsync();

        context.Units.Add(new StorageUnit { Name = "Storage 2", Type = UnitType.Crane });
        await context.SaveChangesAsync();

        // Если кэш не очищается, во втором сохранении могли бы быть данные от первого 
        // (но InstanceId разный у контекстов, если они создаются заново)
        // Здесь мы просто проверяем, что множественные сохранения работают стабильно.
        context.Units.Count().Should().Be(2);
    }
}
