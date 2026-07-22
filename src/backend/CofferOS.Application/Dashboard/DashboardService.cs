using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Contracts;
using CofferOS.Application.Wallets;

namespace CofferOS.Application.Dashboard;

/// <summary>
/// Builds the aggregate dashboard view: total balance across wallets, recent
/// activity and the status of the connected node provider (if any).
/// </summary>
public sealed class DashboardService
{
    private readonly WalletQueryService _walletQueries;
    private readonly IWalletReadStore _readStore;

    public DashboardService(
        WalletQueryService walletQueries,
        IWalletReadStore readStore)
    {
        _walletQueries = walletQueries;
        _readStore = readStore;
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

        return new DashboardDto(summaries.Count, totalBalance, summaries, recent);
    }
}
