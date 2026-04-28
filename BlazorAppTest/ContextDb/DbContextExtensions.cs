using BlazorAppTest.Unit;
using Microsoft.EntityFrameworkCore;

namespace BlazorAppTest;

public static class DbContextExtensions
{
    public static void RegisterUnitEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UnitBase>().UseTptMappingStrategy();
    }
}