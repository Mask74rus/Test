using BlazorAppTest.Audit;
using BlazorAppTest.Domain;
using BlazorAppTest.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BlazorAppTest.Configurator;

public class AppConfigurator : IAppConfigurator
{
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Триггеры и валидация
        services.AddValidatorsFromAssemblyContaining<DomainObjectValidator<Domain.DomainObject>>();
        services.AddSingleton<DatabaseTriggerService>();
        services.AddSingleton<DatabaseTriggerInterceptor>();

        // Регистрируем AuditTrigger в DI, чтобы DatabaseTriggerService мог его разрешить
        services.AddScoped<AuditTrigger>();

        // БД
        services.AddDbContextFactory<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(sp.GetRequiredService<DatabaseTriggerInterceptor>());
        });

        // Репозитории
        services.AddScoped(typeof(IBaseRepository<,>), typeof(BaseRepository<,>));
        services.AddScoped(typeof(IReferenceRepository<>), typeof(ReferenceRepository<>));
    }

    public virtual void ConfigureApp(WebApplication app)
    {
        // Инициализация триггеров
        using var scope = app.Services.CreateScope();

        // 1. Стандартная регистрация из метода расширения
        scope.ServiceProvider.RegisterDomainTriggers();

        // 2. Явная регистрация аудита для базового типа DomainObject
        var triggerService = scope.ServiceProvider.GetRequiredService<DatabaseTriggerService>();
        triggerService.Register<Domain.DomainObject, AuditTrigger>();
    }
}