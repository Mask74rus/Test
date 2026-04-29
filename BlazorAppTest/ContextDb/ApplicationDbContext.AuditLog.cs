using BlazorAppTest.Audit;
using Microsoft.EntityFrameworkCore;

namespace BlazorAppTest;

public partial class ApplicationDbContext
{
    public DbSet<AuditLog> AuditLogs { get; set; }
}