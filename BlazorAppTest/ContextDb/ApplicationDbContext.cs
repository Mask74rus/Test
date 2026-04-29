using BlazorAppTest.Domain;
using BlazorAppTest.DomainObject.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlazorAppTest;

public partial class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Указываем общую схему
        modelBuilder.HasDefaultSchema("test");

        // АВТОМАТИКА: Находим все классы IEntityTypeConfiguration в этой сборке
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ГЛОБАЛЬНЫЕ ПРАВИЛА: Применяем индексы и настройки ключей через метод расширения
        modelBuilder.ApplyGlobalConventions();

        base.OnModelCreating(modelBuilder);
    }
}