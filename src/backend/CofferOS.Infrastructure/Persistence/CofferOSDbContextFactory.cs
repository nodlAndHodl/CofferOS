using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CofferOS.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by the EF Core tools (`dotnet ef migrations ...`).
/// Runtime configuration is supplied via DI in <see cref="DependencyInjection"/>.
/// </summary>
public sealed class CofferOSDbContextFactory : IDesignTimeDbContextFactory<CofferOSDbContext>
{
    public CofferOSDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CofferOSDbContext>()
            .UseSqlite("Data Source=cofferos-design.db")
            .Options;

        return new CofferOSDbContext(options);
    }
}
