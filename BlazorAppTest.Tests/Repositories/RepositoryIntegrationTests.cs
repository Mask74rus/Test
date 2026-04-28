using BlazorAppTest.Domain;
using BlazorAppTest.Repositories;
using BlazorAppTest.Unit;
using FluentAssertions;
using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorAppTest.Tests.Repositories;

public class RepositoryIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly TestDbContextFactory _factory;

    public RepositoryIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // 1. Создаем контейнер служб для внутреннего использования EF Core
        IServiceCollection services = new ServiceCollection()
            .AddEntityFrameworkSqlite()
            .AddSingleton<DatabaseTriggerService>(); // Добавляем сервис, который ищет контекст

        // Регистрируем валидаторы, так как триггеры их вызывают
        services.AddValidatorsFromAssemblyContaining<UnitBaseValidator<UnitBase>>();

        ServiceProvider internalServiceProvider = services.BuildServiceProvider();

        // 2. Настраиваем опции с привязкой к провайдеру
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .UseInternalServiceProvider(internalServiceProvider) // Связываем контекст с сервисами
            .Options;

        using (var context = new ApplicationDbContext(_options))
        {
            context.Database.EnsureCreated();
        }

        // 3. Инициализируем триггеры (чтобы подписки заработали)
        internalServiceProvider.RegisterDomainTriggers();

        // Передаем настроенные опции в твою фабрику
        _factory = new TestDbContextFactory(_options);
    }

    [Fact]
    public async Task AddAsync_Should_Persist_Entity_In_Database()
    {
        // Arrange
        var repository = new BaseRepository<DepartmentUnit, Guid>(_factory);
        var unit = new DepartmentUnit
        {
            Name = "Отдел тестирования",
            Type = UnitType.Other
        };

        // Act
        await repository.AddAsync(unit);

        // Assert
        DepartmentUnit? fromDb = await repository.GetByIdAsync(unit.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Name.Should().Be("Отдел тестирования");
    }

    [Fact]
    public async Task SearchByNameAsync_Should_Return_Filtered_Results()
    {
        // Arrange
        var repository = new ReferenceRepository<StorageUnit>(_factory);
        DateTime past = DateTime.UtcNow.AddMinutes(-1);

        // Добавляем CreatedAt и Type (обязательное поле)
        var s1 = new StorageUnit { Name = "Основной Склад", Type = UnitType.Warehouse, CreatedAt = past };
        var s2 = new StorageUnit { Name = "Холодный Склад", Type = UnitType.Warehouse, CreatedAt = past };
        var s3 = new StorageUnit { Name = "Архив данных", Type = UnitType.Warehouse, CreatedAt = past };

        await repository.AddAsync(s1);
        await repository.AddAsync(s2);
        await repository.AddAsync(s3);

        // Act
        // Используем тот же регистр, что и в базе, чтобы исключить проблемы SQLite с ToLower
        List<StorageUnit> results = await repository.SearchByNameAsync("Склад");

        // Assert
        results.Should().HaveCount(2, "Должно быть найдено два объекта со словом 'Склад'");
    }

    [Fact]
    public async Task GetByCodeAsync_Should_Return_Correct_Entity()
    {
        // Arrange
        var repository = new ReferenceRepository<TransportUnit>(_factory);
        string code = "TR_999";
        var unit = new TransportUnit { Name = "Тягач", Code = code, Type = UnitType.Vehicle };
        await repository.AddAsync(unit);

        // Act
        TransportUnit? result = await repository.GetByCodeAsync(code);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Тягач");
    }

    [Fact]
    public async Task UpdateAsync_Should_Modify_Existing_Record()
    {
        // Arrange
        var repository = new BaseRepository<PositionUnit, Guid>(_factory);
        var unit = new PositionUnit { Name = "Старая позиция", Type = UnitType.Workstation };
        await repository.AddAsync(unit);

        // Act
        unit.Name = "Новая позиция";
        await repository.UpdateAsync(unit);

        // Assert
        PositionUnit? fromDb = await repository.GetByIdAsync(unit.Id);
        fromDb!.Name.Should().Be("Новая позиция");
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Entities()
    {
        // Arrange
        var repository = new BaseRepository<DepartmentUnit, Guid>(_factory);
        DateTime past = DateTime.UtcNow.AddMinutes(-5);

        await repository.AddAsync(new DepartmentUnit { Name = "Dept 1", Type = UnitType.Other, CreatedAt = past });
        await repository.AddAsync(new DepartmentUnit { Name = "Dept 2", Type = UnitType.Other, CreatedAt = past });

        // Act
        List<DepartmentUnit> result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Entity_If_Exists()
    {
        // Arrange
        var repository = new BaseRepository<DepartmentUnit, Guid>(_factory);
        var unit = new DepartmentUnit
        {
            Id = Guid.NewGuid(),
            Name = "To Delete",
            Type = UnitType.Other,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        await repository.AddAsync(unit);

        // Act
        await repository.DeleteAsync(unit.Id);

        // Assert
        DepartmentUnit? deletedUnit = await repository.GetByIdAsync(unit.Id);
        deletedUnit.Should().BeNull("Объект должен быть удален из базы данных");
    }

    [Fact]
    public async Task DeleteAsync_Should_Not_Throw_If_Entity_Does_Not_Exist()
    {
        // Arrange
        var repository = new BaseRepository<DepartmentUnit, Guid>(_factory);
        var nonExistentId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await repository.DeleteAsync(nonExistentId);

        // Assert
        await act.Should().NotThrowAsync("Метод должен безопасно отрабатывать, если ID не найден");
    }

    [Fact]
    public async Task UpdateAsync_Should_Track_Changes_And_Save()
    {
        // Arrange
        var repository = new BaseRepository<DepartmentUnit, Guid>(_factory);
        var unit = new DepartmentUnit
        {
            Name = "Initial Name",
            Type = UnitType.Other,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        await repository.AddAsync(unit);

        // Act
        unit.Name = "Updated Name";
        await repository.UpdateAsync(unit);

        // Assert
        DepartmentUnit? updated = await repository.GetByIdAsync(unit.Id);
        updated!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task GetByCodeAsync_Should_Find_Specific_Reference()
    {
        // Arrange
        // Используем TransportUnit как конкретную реализацию ReferenceBase
        var repository = new ReferenceRepository<TransportUnit>(_factory);
        string targetCode = "TRUCK_001";
        DateTime past = DateTime.UtcNow.AddMinutes(-5);

        var transport = new TransportUnit
        {
            Name = "Карьерный самосвал",
            Code = targetCode,
            Type = UnitType.Vehicle,
            CreatedAt = past
        };
        await repository.AddAsync(transport);

        // Act
        TransportUnit? result = await repository.GetByCodeAsync(targetCode);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(targetCode);
        result.Name.Should().Be("Карьерный самосвал");
    }

    [Fact]
    public async Task GetByCodeAsync_Should_Return_Null_If_Code_Not_Exists()
    {
        // Arrange
        var repository = new ReferenceRepository<TransportUnit>(_factory);

        // Act
        TransportUnit? result = await repository.GetByCodeAsync("NON_EXISTENT_CODE");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchByNameAsync_Should_Handle_Empty_Results()
    {
        // Arrange
        var repository = new ReferenceRepository<DepartmentUnit>(_factory);
        DateTime past = DateTime.UtcNow.AddMinutes(-5);
        await repository.AddAsync(new DepartmentUnit { Name = "Бухгалтерия", Type = UnitType.Other, CreatedAt = past });

        // Act
        List<DepartmentUnit> results = await repository.SearchByNameAsync("Производство");

        // Assert
        results.Should().BeEmpty("Поиск по несуществующему имени должен возвращать пустой список, а не null");
    }

    [Fact]
    public async Task SearchByNameAsync_Should_Be_Case_Sensitive_In_Sqlite()
    {
        // Arrange
        var repository = new ReferenceRepository<StorageUnit>(_factory);
        DateTime past = DateTime.UtcNow.AddMinutes(-5);
        await repository.AddAsync(new StorageUnit { Name = "MAIN_STORAGE", Type = UnitType.Warehouse, CreatedAt = past });

        // Act
        // Напоминание: в SQLite метод Contains без ToLower чувствителен к регистру
        List<StorageUnit> resultFound = await repository.SearchByNameAsync("MAIN");
        List<StorageUnit> resultNotFound = await repository.SearchByNameAsync("main");

        // Assert
        resultFound.Should().HaveCount(1);
        resultNotFound.Should().BeEmpty();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}