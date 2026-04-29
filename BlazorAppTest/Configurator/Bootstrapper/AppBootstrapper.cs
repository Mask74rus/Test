namespace BlazorAppTest.Configurator.AppBootstrapper;

public abstract class AppBootstrapper
{
    public void Run(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Получаем конфигуратор (логика выбора — в наследнике)
        IAppConfigurator configurator = CreateConfigurator();

        // 1. Регистрация сервисов
        configurator.ConfigureServices(builder.Services, builder.Configuration);

        WebApplication app = builder.Build();

        // 2. Настройка пайплайна
        configurator.ConfigureApp(app);

        app.Run();
    }

    // Метод-фабрика, который переопределит ваш модуль
    protected abstract IAppConfigurator CreateConfigurator();
}