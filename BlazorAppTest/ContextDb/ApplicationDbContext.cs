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
        // Указываем схему по умолчанию для всех таблиц этого контекста
        modelBuilder.HasDefaultSchema("test");

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IDomainObjectHasKey<Guid>).IsAssignableFrom(entityType.ClrType))
            {
                PropertyBuilder property = modelBuilder.Entity(entityType.ClrType)
                    .Property("Id")
                    .ValueGeneratedNever(); // Прямое указание базе не генерировать ключ  
            }
        }

        modelBuilder.RegisterUnitEntities();

        base.OnModelCreating(modelBuilder);

        // Автоматически скрывать удаленные элементы для всех, кто реализует ISoftDeletable
        modelBuilder.Entity<ReferenceBase>().HasQueryFilter(u => u.DeletedAt == null);
    }
}