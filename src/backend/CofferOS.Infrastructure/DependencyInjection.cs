using CofferOS.Application.Abstractions.Descriptors;
using CofferOS.Application.Abstractions.Events;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Prices;
using CofferOS.Infrastructure.Descriptors;
using CofferOS.Infrastructure.Events;
using CofferOS.Infrastructure.Persistence;
using CofferOS.Infrastructure.Persistence.Repositories;
using CofferOS.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<ILoanPaymentRepository, LoanPaymentRepository>();
        services.AddScoped<IBitcoinPriceHistoryRepository, BitcoinPriceHistoryRepository>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddSingleton<IDescriptorParser, NBitcoinDescriptorParser>();

        // --- Bitcoin Price Engine -------------------------------------------------
        services.Configure<BitcoinPriceOptions>(configuration.GetSection(BitcoinPriceOptions.SectionName));

        // HttpClient for external price providers (CoinGecko, Coinbase, etc.)
        services.AddHttpClient();

        // Register all available price providers (they are resolved by id at runtime)
        services.AddSingleton<IBitcoinPriceProvider, ManualBitcoinPriceProvider>();
        services.AddSingleton<ManualBitcoinPriceProvider>(); // concrete for IMutable

        services.AddSingleton<IBitcoinPriceProvider, CoinGeckoPriceProvider>(sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<CoinGeckoPriceProvider>>();
            return new CoinGeckoPriceProvider(httpFactory.CreateClient(), logger);
        });

        services.AddSingleton<IBitcoinPriceProvider, CoinbasePriceProvider>(sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<CoinbasePriceProvider>>();
            return new CoinbasePriceProvider(httpFactory.CreateClient(), logger);
        });

        // Choose the active provider based on configuration (falls back to manual)
        services.AddSingleton<IBitcoinPriceProvider>(sp =>
        {
            var opts = sp.GetRequiredService<IOptionsMonitor<BitcoinPriceOptions>>().CurrentValue;
            var all = sp.GetServices<IBitcoinPriceProvider>().ToList();

            var selected = all.FirstOrDefault(p => string.Equals(p.ProviderId, opts.Provider, StringComparison.OrdinalIgnoreCase));
            if (selected is not null) return selected;

            // fallback to manual
            return all.FirstOrDefault(p => p.ProviderId == "manual")
                   ?? new ManualBitcoinPriceProvider();
        });

        // Expose mutable source if the active one supports it (for manual overrides)
        services.AddSingleton<IMutableBitcoinPriceSource>(sp =>
        {
            // Prefer the explicitly registered Manual one for writes
            var manual = sp.GetService<ManualBitcoinPriceProvider>();
            if (manual is not null) return manual;

            // otherwise, if the current provider implements IMutable, use it
            var current = sp.GetRequiredService<IBitcoinPriceProvider>();
            if (current is IMutableBitcoinPriceSource m) return m;

            // last resort: create a fresh manual holder
            return new ManualBitcoinPriceProvider();
        });

        // Application-level price orchestrator
        services.AddScoped<BitcoinPriceService>();

        return services;
    }
}
