using CofferOS.Application.Abstractions.Infrastructure;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using Microsoft.Extensions.Logging;

namespace CofferOS.Application.Infrastructure;

/// <summary>
/// Provides infrastructure health and status information.
/// </summary>
public sealed class InfrastructureService : IInfrastructureService
{
    private readonly IWalletRepository _wallets;
    private readonly IEnumerable<IBitcoinNodeProvider> _nodeProviders;
    private readonly ILogger<InfrastructureService> _logger;
    private readonly Func<ElectrumStatus?> _getElectrumStatus;

    public InfrastructureService(
        IWalletRepository wallets,
        IEnumerable<IBitcoinNodeProvider> nodeProviders,
        ILogger<InfrastructureService> logger,
        Func<ElectrumStatus?> getElectrumStatus)
    {
        _wallets = wallets;
        _nodeProviders = nodeProviders;
        _logger = logger;
        _getElectrumStatus = getElectrumStatus;
    }

    public async Task<InfrastructureStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var walletCount = await GetWalletCountAsync(cancellationToken);
        var bitcoinNode = await GetBitcoinNodeStatusAsync(cancellationToken);
        var electrum = await GetElectrumStatusAsync(cancellationToken);

        return new InfrastructureStatus(walletCount, bitcoinNode, electrum);
    }

    private async Task<int> GetWalletCountAsync(CancellationToken cancellationToken)
    {
        try
        {
            var wallets = await _wallets.GetAllAsync(cancellationToken);
            return wallets.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get wallet count");
            return 0;
        }
    }

    private async Task<BitcoinNodeStatus?> GetBitcoinNodeStatusAsync(CancellationToken cancellationToken)
    {
        var provider = _nodeProviders.FirstOrDefault();
        if (provider is null)
            return null;

        try
        {
            var connection = await provider.TestConnectionAsync(cancellationToken);
            if (!connection.Success)
                return new BitcoinNodeStatus(false, provider.ProviderId, null, null, null, connection.Error);

            var info = await provider.GetBlockchainInfoAsync(cancellationToken);
            return new BitcoinNodeStatus(
                true,
                provider.ProviderId,
                info.Chain,
                info.Blocks,
                info.VerificationProgress,
                null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query node provider {ProviderId}", provider.ProviderId);
            return new BitcoinNodeStatus(false, provider.ProviderId, null, null, null, ex.Message);
        }
    }

    private Task<ElectrumStatus?> GetElectrumStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var status = _getElectrumStatus();
            return Task.FromResult(status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Electrum status");
            return Task.FromResult<ElectrumStatus?>(new ElectrumStatus(false, "electrum", string.Empty, 0, null, null, ex.Message));
        }
    }
}
