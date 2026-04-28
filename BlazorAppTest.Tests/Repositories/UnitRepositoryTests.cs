using BlazorAppTest.Domain;
using BlazorAppTest.Repositories;
using BlazorAppTest.Unit;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace BlazorAppTest.Tests.Repositories;

public class UnitRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly UnitRepository _repository;

    public UnitRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        // 1. СНАЧАЛА регистрируем сам сервис триггеров (обязательно!)
        services.AddSingleton<DatabaseTriggerService>();

        // 2. Инфраструктура (Auth, Logging)
        var authMock = new Mock<AuthenticationStateProvider>();
        authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        services.AddSingleton(authMock.Object);
        services.AddLogging();

        // 3. Интерцептор
        services.AddSingleton<DatabaseTriggerInterceptor>();

        // 4. DbContext и Фабрика
        services.AddDbContext<ApplicationDbContext>((sp, opt) =>
        {
            opt.UseSqlite(_connection);
            opt.AddInterceptors(sp.GetRequiredService<DatabaseTriggerInterceptor>());
        });

        services.AddSingleton<IDbContextFactory<ApplicationDbContext>>(sp =>
            new TestDbContextFactory(sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>()));

        // 5. Репозиторий и валидаторы
        services.AddScoped<UnitRepository>();
        services.AddValidatorsFromAssemblyContaining<UnitBaseValidator<UnitBase>>();

        _serviceProvider = services.BuildServiceProvider();

        // 6. ТЕПЕРЬ можно вызывать регистрацию (сервис уже есть в контейнере)
        _serviceProvider.RegisterDomainTriggers();

        // 7. Инициализация БД и репозитория
        using (var context = _serviceProvider.GetRequiredService<ApplicationDbContext>())
        {
            context.Database.EnsureCreated();
        }

        _repository = _serviceProvider.GetRequiredService<UnitRepository>();
    }

    // Вспомогательный класс для тестов (если его еще нет в проекте)
    private class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }

    [Fact]
    public async Task GetRootNodesAsync_Should_Return_Only_Top_Level_Units()
    {
        // Arrange
        DateTime past = DateTime.UtcNow.AddMinutes(-5);
        var root = new DepartmentUnit { Name = "Корень", Type = UnitType.Other, CreatedAt = past };
        await _repository.AddAsync(root);

        var child = new PositionUnit { Name = "Дочка", Type = UnitType.Workstation, ParentId = root.Id, CreatedAt = past };
        await _repository.AddAsync(child);

        // Act
        List<UnitBase> roots = await _repository.GetRootNodesAsync();

        // Assert
        roots.Should().HaveCount(1);
        roots.First().Name.Should().Be("Корень");
    }

    [Fact]
    public async Task MoveAsync_Should_Update_ParentId_Successfully()
    {
        // Arrange
        DateTime past = DateTime.UtcNow.AddMinutes(-5);
        var unitA = new DepartmentUnit { Name = "Подразделение A", Type = UnitType.Other, CreatedAt = past };
        var unitB = new DepartmentUnit { Name = "Подразделение B", Type = UnitType.Other, CreatedAt = past };
        await _repository.AddAsync(unitA);
        await _repository.AddAsync(unitB);

        // Act
        await _repository.MoveAsync(unitB.Id, unitA.Id);

        // Assert
        UnitBase? updatedB = await _repository.GetByIdAsync(unitB.Id);
        updatedB!.ParentId.Should().Be(unitA.Id);
    }

    [Fact]
    public async Task MoveAsync_Should_Throw_When_Trigger_Detects_Circular_Dependency()
    {
        // Arrange
        DateTime past = DateTime.UtcNow.AddMinutes(-5);
        var unitA = new DepartmentUnit { Id = Guid.NewGuid(), Name = "Подразделение A", Type = UnitType.Other, CreatedAt = past };
        await _repository.AddAsync(unitA);

        // Act
        // Пытаемся переместить объект в самого себя
        Func<Task> act = async () => await _repository.MoveAsync(unitA.Id, unitA.Id);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>()
            .WithMessage("*Циклическая зависимость*");
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}