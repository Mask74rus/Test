using BlazorAppTest.Audit;
using BlazorAppTest.Components;
using BlazorAppTest.Domain;
using MudBlazor.Services;

namespace BlazorAppTest.Configurator;

public class WebAppConfigurator : AppConfigurator
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        base.ConfigureServices(services, configuration); // Сначала всё из Base

        services.AddRazorComponents().AddInteractiveServerComponents();
        services.AddMudServices();
    }

    public override void ConfigureApp(WebApplication app)
    {
        base.ConfigureApp(app); // Сначала триггеры

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();
        app.UseAntiforgery();
        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
    }
}