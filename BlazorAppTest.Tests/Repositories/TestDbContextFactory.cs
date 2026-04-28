using Microsoft.EntityFrameworkCore;

namespace BlazorAppTest.Tests.Repositories;

// Вспомогательный класс для имитации фабрики в тестах
public class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
    : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext()
    {
        // Каждый вызов возвращает новый экземпляр, как и в реальности
        return new ApplicationDbContext(options);
    }
}