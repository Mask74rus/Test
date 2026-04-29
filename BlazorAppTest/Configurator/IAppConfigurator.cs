namespace BlazorAppTest.Configurator;

public interface IAppConfigurator
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    void ConfigureApp(WebApplication app);
}