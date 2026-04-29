namespace BlazorAppTest.Configurator.AppBootstrapper;

public class ModuleBootstrapper : AppBootstrapper
{
    protected override IAppConfigurator CreateConfigurator()
        => new ModuleConfigurator(); // Здесь указываем финальный уровень
}