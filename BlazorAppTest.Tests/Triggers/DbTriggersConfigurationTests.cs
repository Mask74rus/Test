using BlazorAppTest.Domain;
using BlazorAppTest.Unit;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

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

        // 1. Инфраструктура и триггеры
        services.AddEntityFrameworkSqlite();
        services.AddSingleton<DatabaseTriggerService>();

        // 2. Имитация авторизации (нужна для интерцептора)
        var authMock = new Mock<AuthenticationStateProvider>();
        authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "TestUser")], "Test"))));
        services.AddSingleton(authMock.Object);

        // 3. Регистрация Интерцептора
        services.AddSingleton<DatabaseTriggerInterceptor>();

        // 4. Регистрация контекста С ИНТЕРЦЕПТОРОМ
        services.AddDbContext<ApplicationDbContext>((sp, opt) =>
        {
            opt.UseSqlite(_connection);
            // ПОДКЛЮЧАЕМ ИНТЕРЦЕПТОР - без этого SaveChangesAsync ничего не проверит
            opt.AddInterceptors(sp.GetRequiredService<DatabaseTriggerInterceptor>());
        });

        services.AddValidatorsFromAssemblyContaining<UnitBaseValidator<UnitBase>>();


        var allValidators = services.Where(x => x.ServiceType.IsGenericType &&
                                                x.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>)).ToList();

        foreach (var v in allValidators)
            Console.WriteLine($"Registered validator: {v.ServiceType.Name} -> {v.ImplementationType?.Name}");

        _serviceProvider = services.BuildServiceProvider();
        _options = _serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        // Инициализируем триггеры
        _serviceProvider.RegisterDomainTriggers();

        using var context = new ApplicationDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Configuration_Should_Link_FluentValidation_To_DatabaseSave()
    {
        // Используйте провайдер, чтобы создать контекст точно с теми же настройками, 
        // что были сконфигурированы в конструкторе теста.
        await using var context = _serviceProvider.GetRequiredService<ApplicationDbContext>();

        var invalidUnit = new DepartmentUnit
        {
            Id = Guid.NewGuid(),
            Name = "", // Невалидно
            Type = UnitType.Other
        };
        context.Units.Add(invalidUnit);

        Func<Task> act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Configuration_Should_Link_HierarchyValidation_To_DatabaseSave()
    {
        // Arrange
        await using var context = new ApplicationDbContext(_options);

        var unitA = new DepartmentUnit
        {
            Id = Guid.NewGuid(),
            Name = "Valid Name",
            Type = UnitType.Other
        };

        context.Units.Add(unitA);
        await context.SaveChangesAsync();

        // Act: Создаем цикл
        unitA.ParentId = unitA.Id;
        context.Units.Update(unitA);

        Func<Task> act = async () => await context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>()
            .WithMessage("*Циклическая зависимость*");
    }

    public void Dispose() => _connection.Dispose();
}