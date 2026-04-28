using BlazorAppTest.Unit;
using Microsoft.EntityFrameworkCore;

namespace BlazorAppTest;

public partial class ApplicationDbContext
{
    public DbSet<UnitBase> Units => Set<UnitBase>();
    public DbSet<DepartmentUnit> DepartmentUnits => Set<DepartmentUnit>();
    public DbSet<ProductionUnit> ProductionUnits => Set<ProductionUnit>();
    public DbSet<TransportUnit> TransportUnits => Set<TransportUnit>();
    public DbSet<StorageUnit> StorageUnits => Set<StorageUnit>();
    public DbSet<PositionUnit> PositionUnits => Set<PositionUnit>();
}