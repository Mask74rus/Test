using BlazorAppTest.Components;
using BlazorAppTest.Domain;
using BlazorAppTest.Repositories;
using BlazorAppTest.Unit;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

namespace BlazorAppTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Подключаем PostgreSQL
            builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Регистрация валидаторов
            builder.Services.AddValidatorsFromAssemblyContaining<UnitBaseValidator<UnitBase>>();

            // Регистрируем сервис для работы с триггерами
            builder.Services.AddSingleton<DatabaseTriggerService>();

            // Регистрация репозиториев 
            builder.Services.AddScoped(typeof(IBaseRepository<,>), typeof(BaseRepository<,>));
            builder.Services.AddScoped(typeof(IReferenceRepository<>), typeof(ReferenceRepository<>));
            builder.Services.AddScoped<IUnitRepository, UnitRepository>();

            builder.Services.AddMudServices();

            WebApplication app = builder.Build();

            // Инициализируем триггеры при старте приложения
            // Используем Scope, чтобы безопасно получить доступ к сервисам
            using (IServiceScope scope = app.Services.CreateScope())
            {
                scope.ServiceProvider.RegisterDomainTriggers();
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
