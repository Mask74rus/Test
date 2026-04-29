using BlazorAppTest.Repositories;

namespace BlazorAppTest.Configurator;

public class ModuleConfigurator : WebAppConfigurator
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        base.ConfigureServices(services, configuration);

        // Специфичные репозитории проекта
        services.AddScoped<IUnitRepository, UnitRepository>();
    }

    public override void ConfigureApp(WebApplication app)
    {
        base.ConfigureApp(app);
        // Здесь можно добавить специфичные регистрации триггеров через DatabaseTriggerService,
        // если они не подтянулись автоматически через RegisterDomainTriggers
    }
}