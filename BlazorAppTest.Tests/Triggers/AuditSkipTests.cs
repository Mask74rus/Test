using BlazorAppTest.Domain;
using BlazorAppTest.DomainObject.Interface;
using BlazorAppTest.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace BlazorAppTest.Tests.Triggers;

public class AuditSkipTests
{
    [Fact]
    public async Task CaptureChanges_ShouldIgnoreSkipAudit_AndHandleSoftDelete()
    {
        // 1. Arrange - Настройка базы и сервисов
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);
        var userName = "TestUser";

        // Настройка авторизации (чтобы GetUserNameAsync вернул TestUser)
        var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, userName)
        }, "TestAuth")));

        var authProviderMock = new Mock<AuthenticationStateProvider>();
        authProviderMock.Setup(p => p.GetAuthenticationStateAsync()).ReturnsAsync(authState);

        // Настройка DI для интерцептора
        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeMock = new Mock<IServiceScope>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        // Настраиваем цепочку: ServiceProvider -> ScopeFactory -> Scope -> ServiceProvider -> AuthProvider
        serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(AuthenticationStateProvider))).Returns(authProviderMock.Object);

        var triggerService = new DatabaseTriggerService(scopeFactoryMock.Object);
        var interceptor = new DatabaseTriggerInterceptor(triggerService, serviceProviderMock.Object);

        // Подготовка данных
        var auditable = new AuditableEntity { Id = Guid.NewGuid(), Name = "Visible" };
        var nonAuditable = new NonAuditableEntity { Id = Guid.NewGuid(), Secret = "Hidden" };

        context.Add(auditable);
        context.Add(nonAuditable);
        await context.SaveChangesAsync();

        // 2. Act - Имитация удаления
        context.Remove(auditable);

        // Запускаем SavingChangesAsync (здесь проставится DeletedBy и DeletedAt)
        await interceptor.SavingChangesAsync(
            new DbContextEventData(null, null, context),
            new InterceptionResult<int>());

        var captured = CaptureChangesViaReflection(interceptor, context, userName);

        // 3. Assert
        captured.Should().NotBeNull();

        // Проверка игнорирования ISkipAudit
        captured.Should().HaveCount(1, "сущность ISkipAudit не должна попасть в аудит");
        captured.Any(c => c.Entity is NonAuditableEntity).Should().BeFalse();

        // Проверка Soft Delete
        var softDeletedEntry = captured.FirstOrDefault(c => c.Entity is AuditableEntity);
        softDeletedEntry.Should().NotBeNull();
        softDeletedEntry!.State.Should().Be(EntityStateChangeEnum.SoftDeleted);

        var entity = (AuditableEntity)softDeletedEntry.Entity;
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedBy.Should().Be(userName); // Теперь здесь будет "TestUser"
    }

    private List<ChangeEntryModel> CaptureChangesViaReflection(DatabaseTriggerInterceptor interceptor, DbContext context, string user)
    {
        var method = typeof(DatabaseTriggerInterceptor)
            .GetMethod("CaptureChanges", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return (List<ChangeEntryModel>)method.Invoke(interceptor, new object[] { context, user })!;
    }
}

// Вспомогательные классы для теста
public class TestDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    public DbSet<AuditableEntity> AuditableEntities { get; set; }
    public DbSet<NonAuditableEntity> NonAuditableEntities { get; set; }
}

public class AuditableEntity : Domain.DomainObject, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

public class NonAuditableEntity : Domain.DomainObject, ISkipAudit
{
    public string Secret { get; set; } = string.Empty;
}