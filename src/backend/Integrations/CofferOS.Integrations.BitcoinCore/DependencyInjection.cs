using CofferOS.Application.Abstractions.Descriptors;
using CofferOS.Application.Abstractions.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CofferOS.Integrations.BitcoinCore;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Bitcoin Core provider plugin, but only if it is enabled in
    /// configuration. When disabled, CofferOS runs happily with no node connected.
    /// </summary>
    public static IServiceCollection AddBitcoinCoreIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(BitcoinCoreOptions.SectionName);
        services.Configure<BitcoinCoreOptions>(section);

        var options = section.Get<BitcoinCoreOptions>();
        if (options is null || !options.Enabled)
            return services;

        services.AddSingleton<BitcoinCoreRpcClient>();

        // Register the single implementation behind every contract it fulfils.
        services.AddSingleton<BitcoinCoreProvider>();
        services.AddSingleton<IBitcoinNodeProvider>(sp => sp.GetRequiredService<BitcoinCoreProvider>());
        services.AddSingleton<ITransactionProvider>(sp => sp.GetRequiredService<BitcoinCoreProvider>());
        services.AddSingleton<IUtxoProvider>(sp => sp.GetRequiredService<BitcoinCoreProvider>());

        return services;
    }

    /// <summary>
    /// Registers an Electrum X1 server as the IUtxoProvider source when enabled.
    /// If Bitcoin Core also registered IUtxoProvider, this registration wins because it is last.
    /// </summary>
    public static IServiceCollection AddElectrumServerIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ElectrumOptions.SectionName);
        services.Configure<ElectrumOptions>(section);

        var options = section.Get<ElectrumOptions>();
        if (options is null || !options.Enabled)
            return services;

        services.AddSingleton<ElectrumServerProvider>();
        services.AddSingleton<IUtxoProvider>(sp => sp.GetRequiredService<ElectrumServerProvider>());
        services.AddSingleton<IWalletHistoryProvider>(sp => sp.GetRequiredService<ElectrumServerProvider>());

        return services;
    }
}
