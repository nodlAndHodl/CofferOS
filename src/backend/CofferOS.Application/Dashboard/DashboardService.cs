using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Contracts;
using CofferOS.Application.Wallets;
using Microsoft.Extensions.Logging;

namespace CofferOS.Application.Dashboard;

/// <summary>
/// Builds the aggregate dashboard view: total balance across wallets, recent
/// activity and the status of the connected node provider (if any).
/// </summary>
public sealed class DashboardService
{
    private readonly WalletQueryService _walletQueries;
    private readonly IWalletReadStore _readStore;
    private readonly IEnumerable<IBitcoinNodeProvider> _nodeProviders;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        WalletQueryService walletQueries,
        IWalletReadStore readStore,
        IEnumerable<IBitcoinNodeProvider> nodeProviders,
        ILogger<DashboardService> logger)
    {
        _walletQueries = walletQueries;
        _readStore = readStore;
        _nodeProviders = nodeProviders;
        _logger = logger;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var summaries = await _walletQueries.GetSummariesAsync(cancellationToken);

        long confirmed = summaries.Sum(s => s.Balance.ConfirmedSats);
        long unconfirmed = summaries.Sum(s => s.Balance.UnconfirmedSats);
        long total = confirmed + unconfirmed;
        var totalBalance = new BalanceDto(confirmed, unconfirmed, total, (decimal)total / 100_000_000m);

        var recent = new List<TransactionDto>();
        foreach (var summary in summaries)
        {
            var txs = await _readStore.GetTransactionsAsync(summary.Id, cancellationToken);
            recent.AddRange(txs.Select(t => new TransactionDto(
                t.TxId, t.NetAmountSats, t.FeeSats, t.Direction.ToString(), t.Confirmations, t.BlockHeight, t.Timestamp)));
        }

        recent = recent
            .OrderByDescending(t => t.Timestamp ?? DateTimeOffset.MinValue)
            .Take(10)
            .ToList();

        var node = await GetNodeStatusAsync(cancellationToken);

        return new DashboardDto(summaries.Count, totalBalance, summaries, recent, node);
    }

    private async Task<NodeStatusDto> GetNodeStatusAsync(CancellationToken cancellationToken)
    {
        var provider = _nodeProviders.FirstOrDefault();
        if (provider is null)
            return new NodeStatusDto(false, "none", null, null, null, "No node provider configured");

        try
        {
            var connection = await provider.TestConnectionAsync(cancellationToken);
            if (!connection.Success)
                return new NodeStatusDto(false, provider.ProviderId, null, null, null, connection.Error);

            var info = await provider.GetBlockchainInfoAsync(cancellationToken);
            return new NodeStatusDto(true, provider.ProviderId, info.Chain, info.Blocks, info.VerificationProgress, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query node provider {ProviderId}", provider.ProviderId);
            return new NodeStatusDto(false, provider.ProviderId, null, null, null, ex.Message);
        }
    }
}
