using CofferOS.Application.Abstractions.Descriptors;
using CofferOS.Application.Abstractions.Events;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Infrastructure.Descriptors;
using CofferOS.Infrastructure.Events;
using CofferOS.Infrastructure.Persistence;
using CofferOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CofferOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var dbPath = configuration.GetConnectionString("Default")
                     ?? configuration["Database:Path"]
                     ?? "data/cofferos.db";

        // If a bare file path was provided, turn it into a SQLite connection string.
        var connectionString = dbPath.Contains("Data Source", StringComparison.OrdinalIgnoreCase)
            ? dbPath
            : $"Data Source={dbPath}";

        services.AddDbContext<CofferOSDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<IMetadataRepository, MetadataRepository>();
        services.AddScoped<ITimelineEventRepository, TimelineEventRepository>();
        services.AddScoped<IWalletReadStore, WalletReadStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddSingleton<IDescriptorParser, NBitcoinDescriptorParser>();

        return services;
    }
}
