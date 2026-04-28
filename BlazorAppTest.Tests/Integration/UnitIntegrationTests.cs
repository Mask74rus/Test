using BlazorAppTest.Domain;
using BlazorAppTest.Tests.Repositories;
using BlazorAppTest.Unit;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace BlazorAppTest.Tests.Integration;

public class UnitIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly IServiceProvider _internalServiceProvider;

    public UnitIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        // 1. Инфраструктура
        services.AddSingleton<DatabaseTriggerService>();
        services.AddLogging();

        // 2. Имитация авторизации (обязательно для работы перехватчика)
        var authMock = new Mock<AuthenticationStateProvider>();
        authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        services.AddSingleton(authMock.Object);

        // 3. Регистрация перехватчика
        services.AddSingleton<DatabaseTriggerInterceptor>();

        // 4. Настройка опций (сразу с перехватчиком)
        services.AddDbContext<ApplicationDbContext>((sp, opt) =>
        {
            opt.UseSqlite(_connection);
            opt.AddInterceptors(sp.GetRequiredService<DatabaseTriggerInterceptor>());
        });

        // 5. Фабрика (теперь берет опции из контейнера правильно)
        services.AddSingleton<IDbContextFactory<ApplicationDbContext>>(sp =>
            new TestDbContextFactory(sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>()));

        services.AddValidatorsFromAssemblyContaining<UnitBaseValidator<UnitBase>>();

        _internalServiceProvider = services.BuildServiceProvider();
        _options = _internalServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        // 6. Инициализация триггеров
        _internalServiceProvider.RegisterDomainTriggers();

        // Создание схемы
        using var context = new ApplicationDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task StorageUnit_Should_Persist_In_Both_Tables_Via_TPT()
    {
        // Arrange
        var storageId = Guid.NewGuid();
        var storage = new StorageUnit
        {
            Id = storageId,
            Name = "Холодильный склад",
            Code = "COLD_01",
            Type = UnitType.Warehouse,
            IsAutomaticArchiving = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        await using (var context = new ApplicationDbContext(_options))
        {
            context.StorageUnits.Add(storage);
            await context.SaveChangesAsync();
        }

        // Assert: Проверяем, что данные физически разошлись по таблицам TPT
        await using (var context = new ApplicationDbContext(_options))
        {
            // Проверяем через базовый тип (проверка существования в главной таблице Units)
            UnitBase? baseUnit = await context.Units.FindAsync(storageId);
            baseUnit.Should().NotBeNull();
            baseUnit!.Name.Should().Be("Холодильный склад");
            baseUnit.Kind.Should().Be(UnitKind.Storage);

            // Проверяем через конкретный тип (проверка специфичного поля в Units_Storages)
            StorageUnit? specificUnit = await context.StorageUnits.FindAsync(storageId);
            specificUnit.Should().NotBeNull();
            specificUnit!.IsAutomaticArchiving.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SaveChanges_Should_Fail_If_Business_Rules_Are_Violated()
    {
        // Arrange
        await using var context = new ApplicationDbContext(_options);

        // Создаем невалидный объект (Транспорт с типом "Склад")
        var invalidTransport = new TransportUnit
        {
            Name = "Ошибка",
            Type = UnitType.Warehouse, // Это вызовет ошибку в TransportUnitValidator
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        context.TransportUnits.Add(invalidTransport);

        // Act & Assert
        // DatabaseTriggerService должен выбросить исключение до записи в БД
        Func<Task<int>> act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<OperationCanceledException>()
            .WithMessage("*не относится к транспортному оборудованию*");
    }

    [Fact]
    public async Task UnitHierarchy_Should_Correctly_Link_Parent_And_Children()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var rootDepartment = new DepartmentUnit
        {
            Id = rootId,
            Name = "Головной офис",
            Type = UnitType.Other,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var childPosition = new PositionUnit
        {
            Id = childId,
            Name = "Рабочее место №1",
            Type = UnitType.Workstation,
            ParentId = rootId, // Устанавливаем связь через FK
            OrderNo = 1,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        await using (var context = new ApplicationDbContext(_options))
        {
            context.Units.AddRange(rootDepartment, childPosition);
            await context.SaveChangesAsync();
        }

        // Assert
        await using (var context = new ApplicationDbContext(_options))
        {
            // 1. Проверяем, что ребенок видит родителя
            PositionUnit? childFromDb = await context.PositionUnits
                .Include(x => x.Parent)
                .FirstOrDefaultAsync(x => x.Id == childId);

            childFromDb.Should().NotBeNull();
            childFromDb!.ParentId.Should().Be(rootId);
            childFromDb.Parent.Should().NotBeNull();
            childFromDb.Parent!.Name.Should().Be("Головной офис");

            // 2. Проверяем, что родитель видит список детей
            DepartmentUnit? rootFromDb = await context.DepartmentUnits
                .Include(x => x.Children)
                .FirstOrDefaultAsync(x => x.Id == rootId);

            rootFromDb.Should().NotBeNull();
            rootFromDb!.Children.Should().HaveCount(1);
            rootFromDb.Children.First().Id.Should().Be(childId);
            rootFromDb.Children.First().Name.Should().Be("Рабочее место №1");
        }
    }

    [Fact]
    public async Task UnitHierarchy_Should_Prevent_Circular_Reference_Via_Validator()
    {
        // Arrange
        var id = Guid.NewGuid();
        await using var context = new ApplicationDbContext(_options);

        var selfReferencedUnit = new DepartmentUnit
        {
            Id = id,
            Name = "Ошибка цикла",
            Type = UnitType.Other,
            ParentId = id, // Прямая циклическая ссылка (сам на себя)
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        context.DepartmentUnits.Add(selfReferencedUnit);

        // Act & Assert
        // Используем делегат для вызова сохранения
        Func<Task<int>> act = async () => await context.SaveChangesAsync();

        // Теперь триггер выбрасывает общее сообщение о циклической зависимости
        await act.Should().ThrowAsync<OperationCanceledException>()
            .WithMessage("*Циклическая зависимость*");
    }

    [Fact]
    public async Task UnitHierarchy_Should_Prevent_Deep_Circular_Reference_ABC_A()
    {
        // 1. Arrange: Создаем цепочку A -> B -> C
        var unitA = new DepartmentUnit
        {
            Id = Guid.NewGuid(),
            Name = "Подразделение A",
            Type = UnitType.Other,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var unitB = new DepartmentUnit
        {
            Id = Guid.NewGuid(),
            Name = "Подразделение B",
            Type = UnitType.Other,
            ParentId = unitA.Id, // B под A
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var unitC = new DepartmentUnit
        {
            Id = Guid.NewGuid(),
            Name = "Подразделение C",
            Type = UnitType.Other,
            ParentId = unitB.Id, // C под B
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        await using (var context = new ApplicationDbContext(_options))
        {
            context.Units.AddRange(unitA, unitB, unitC);
            await context.SaveChangesAsync();
        }

        // 2. Act: Пытаемся сделать C родителем для A. 
        // Получится петля: A -> B -> C -> A
        await using (var context = new ApplicationDbContext(_options))
        {
            UnitBase? topUnit = await context.Units.FindAsync(unitA.Id);
            topUnit!.ParentId = unitC.Id; // Замыкаем кольцо

            Func<Task<int>> act = async () => await context.SaveChangesAsync();

            // 3. Assert: DatabaseTriggerService должен выбросить исключение
            await act.Should().ThrowAsync<OperationCanceledException>()
                .WithMessage("*Циклическая зависимость*");
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}