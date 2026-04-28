using BlazorAppTest.Domain;
using BlazorAppTest.Repositories;
using BlazorAppTest.Unit;
using FluentAssertions;
using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorAppTest.Tests.Repositories;

public class UnitRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly UnitRepository _repository;

    public UnitRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection()
            .AddEntityFrameworkSqlite()
            .AddSingleton<DatabaseTriggerService>()
            .AddLogging();

        // 1. Регистрируем саму фабрику, которую будет использовать триггер
        // Используем ваш TestDbContextFactory
        services.AddSingleton<IDbContextFactory<ApplicationDbContext>>(sp =>
            new TestDbContextFactory(_options));

        services.AddValidatorsFromAssemblyContaining<UnitBaseValidator<UnitBase>>();

        _serviceProvider = services.BuildServiceProvider();
        _serviceProvider.RegisterDomainTriggers();

        // ... остальной код настройки _options ...
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .UseInternalServiceProvider(_serviceProvider)
            .Options;

        using var context = new ApplicationDbContext(_options);
        context.Database.EnsureCreated();

        // Передаем фабрику в репозиторий
        var factory = new TestDbContextFactory(_options);
        _repository = new UnitRepository(factory);
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