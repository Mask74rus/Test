using BlazorAppTest.Domain;
using BlazorAppTest.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BlazorAppTest.Configurator;

public class BaseConfigurator : IAppConfigurator
{
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Триггеры и валидация
        services.AddValidatorsFromAssemblyContaining<DomainObjectValidator<Domain.DomainObject>>();
        services.AddSingleton<DatabaseTriggerService>();
        services.AddSingleton<DatabaseTriggerInterceptor>();

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
        scope.ServiceProvider.RegisterDomainTriggers();
    }
}