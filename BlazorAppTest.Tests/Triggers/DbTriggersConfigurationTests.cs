using BlazorAppTest.Domain;
using BlazorAppTest.Tests.Repositories;
using BlazorAppTest.Unit;
using FluentAssertions;
using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorAppTest.Tests.Triggers;

public class DbTriggersConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly IServiceProvider _serviceProvider;

    public DbTriggersConfigurationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        // 1. Регистрируем инфраструктуру
        services.AddEntityFrameworkSqlite();
        services.AddSingleton<DatabaseTriggerService>();
        services.AddLogging();

        // 2. Регистрируем контекст и фабрику (нужна для триггера иерархии)
        services.AddDbContext<ApplicationDbContext>(opt => opt.UseSqlite(_connection));
        services.AddSingleton<IDbContextFactory<ApplicationDbContext>>(sp =>
            new TestDbContextFactory(sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>()));

        // 3. Регистрируем валидаторы из сборки
        services.AddValidatorsFromAssemblyContaining<UnitBaseValidator<UnitBase>>();

        _serviceProvider = services.BuildServiceProvider();
        _options = _serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        // 4. ГЛАВНОЕ: Вызываем регистрацию наших триггеров
        _serviceProvider.RegisterDomainTriggers();

        // Создаем схему БД
        using var context = new ApplicationDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Configuration_Should_Link_FluentValidation_To_DatabaseSave()
    {
        // Проверяем, что триггер подхватил FluentValidation (на примере пустого имени)
        // Arrange
        await using var context = new ApplicationDbContext(_options);
        var invalidUnit = new DepartmentUnit
        {
            Name = "", // Ошибка: имя обязательно
            Type = UnitType.Other
        };
        context.DepartmentUnits.Add(invalidUnit);

        // Act
        Func<Task> act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>()
            .WithMessage("*Наименование обязательно*");
    }

    [Fact]
    public async Task Configuration_Should_Link_HierarchyValidation_To_DatabaseSave()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);

        // Имя должно быть валидным (минимум 2 символа), чтобы пройти первый уровень валидации
        var unitA = new DepartmentUnit
        {
            Id = Guid.NewGuid(),
            Name = "Главный отдел", // Исправлено: > 2 символов
            Type = UnitType.Other
        };

        context.Units.Add(unitA);
        await context.SaveChangesAsync();

        // Теперь провоцируем цикл
        unitA.ParentId = unitA.Id;

        // Act
        Func<Task> act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>()
            .WithMessage("*Циклическая зависимость*");
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}