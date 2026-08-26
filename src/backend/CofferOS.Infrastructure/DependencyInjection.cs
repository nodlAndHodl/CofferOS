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
using Microsoft.Extensions.Logging;
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

        services.AddDbContext<CofferOSDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            // Allow startup to proceed even if the model has drifted from the last snapshot.
            // Existing migration files on disk will still be applied.
            // TODO: Add a new migration to bring the snapshot in sync, then remove this ignore.
            options.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<IMetadataRepository, MetadataRepository>();
        services.AddScoped<ITimelineEventRepository, TimelineEventRepository>();
        services.AddScoped<IWalletReadStore, WalletReadStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<ILoanPaymentRepository, LoanPaymentRepository>();
        services.AddScoped<ILoanPriceSnapshotRepository, LoanPriceSnapshotRepository>();
        services.AddScoped<ILoanCollateralTransactionRepository, LoanCollateralTransactionRepository>();
        services.AddScoped<IRetirementAccountRepository, RetirementAccountRepository>();
        services.AddScoped<IBitcoinPriceHistoryRepository, BitcoinPriceHistoryRepository>();
        services.AddScoped<ICostBasisRepository, CostBasisRepository>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddSingleton<IDescriptorParser, NBitcoinDescriptorParser>();

        // --- Bitcoin Price Engine -------------------------------------------------
        services.Configure<BitcoinPriceOptions>(configuration.GetSection(BitcoinPriceOptions.SectionName));

        // HttpClient for external price providers (CoinGecko, Coinbase, etc.)
        services.AddHttpClient();
        services.AddHttpClient("BitcoinPrice", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(8);
            c.DefaultRequestHeaders.Add("Accept", "application/json");
            c.DefaultRequestHeaders.UserAgent.ParseAdd("CofferOS/1.0 (Bitcoin price fetcher; https://github.com/nodlAndHodl/CofferOS)");
        });

        // Register all available price providers as concrete singletons.
        // They are resolved by concrete type to avoid circular IBitcoinPriceProvider resolution.
        services.AddSingleton<ManualBitcoinPriceProvider>();

        services.AddSingleton<CoinGeckoPriceProvider>(sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<CoinGeckoPriceProvider>>();
            return new CoinGeckoPriceProvider(httpFactory.CreateClient("BitcoinPrice"), logger);
        });

        // Choose the active provider based on configuration (falls back to manual).
        // Resolves by concrete type — never call GetServices<IBitcoinPriceProvider> here
        // because this factory IS an IBitcoinPriceProvider registration and that would deadlock.
        services.AddSingleton<IBitcoinPriceProvider>(sp =>
        {
            var opts = sp.GetRequiredService<IOptionsMonitor<BitcoinPriceOptions>>().CurrentValue;
            var providerId = opts.Provider?.ToLowerInvariant() ?? "manual";
            return providerId switch
            {
                "coingecko" => (IBitcoinPriceProvider)sp.GetRequiredService<CoinGeckoPriceProvider>(),
                _ => sp.GetRequiredService<ManualBitcoinPriceProvider>(),
            };
        });

        // Expose mutable source for manual price overrides
        services.AddSingleton<IMutableBitcoinPriceSource>(sp => sp.GetRequiredService<ManualBitcoinPriceProvider>());

        // Exchange rate provider always backed by the CoinGecko singleton (cached rates)
        services.AddSingleton<IExchangeRateProvider>(sp => sp.GetRequiredService<CoinGeckoPriceProvider>());

        // Application-level price orchestrator
        services.AddScoped<BitcoinPriceService>();

        // Historical price fetching service via CoinGecko market_chart/range (arbitrary dates, multi-currency)
        services.AddScoped<CoinGeckoHistoricalPriceService>(sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<CoinGeckoHistoricalPriceService>>();
            return new CoinGeckoHistoricalPriceService(httpFactory.CreateClient("BitcoinPrice"), logger);
        });

        return services;
    }
}
