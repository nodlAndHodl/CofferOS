using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Contracts;
using CofferOS.Application.Wallets;
using CofferOS.Domain.Common;

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

        var recent = await GetRecentActivityAsync(0, 10, cancellationToken);

        return new DashboardDto(summaries.Count, totalBalance, summaries, recent);
    }

    /// <summary>
    /// Returns the most recent global transaction activity, enriched with the originating
    /// wallet name and any transaction labels/tags. Results are capped to the latest 100
    /// transactions so the dashboard stays responsive while still supporting pagination.
    /// </summary>
    public async Task<RecentActivityPageDto> GetRecentActivityAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        const int MaxHistory = 100;
        take = Math.Min(take, MaxHistory);

        var summaries = await _walletQueries.GetSummariesAsync(cancellationToken);
        var walletNames = summaries.ToDictionary(s => s.Id, s => s.Name);

        var transactions = await _readStore.GetRecentTransactionsAsync(skip, take, cancellationToken);
        var total = await _readStore.GetRecentTransactionCountAsync(cancellationToken);
        total = Math.Min(total, MaxHistory);

        var walletIds = transactions.Select(t => t.WalletId).Distinct().ToList();
        var labelsByTx = new Dictionary<(Guid WalletId, string Reference), string>();
        var tagsByTx = new Dictionary<(Guid WalletId, string Reference), List<string>>();

        foreach (var walletId in walletIds)
        {
            var labels = await _readStore.GetLabelsAsync(walletId, cancellationToken);
            foreach (var label in labels.Where(l => l.Target == LabelTarget.Transaction))
                labelsByTx[(walletId, label.Reference)] = label.Text;

            var tags = await _readStore.GetTagsAsync(walletId, cancellationToken);
            foreach (var grouping in tags.Where(t => t.Target == LabelTarget.Transaction).GroupBy(t => t.Reference))
                tagsByTx[(walletId, grouping.Key)] = grouping.Select(t => t.Value).ToList();
        }

        var items = transactions.Select(t => new RecentActivityItemDto(
            t.TxId,
            t.NetAmountSats,
            t.BlockHeight,
            t.Timestamp,
            walletNames.GetValueOrDefault(t.WalletId, "Unknown"),
            labelsByTx.TryGetValue((t.WalletId, t.TxId), out var label) ? label : null,
            tagsByTx.TryGetValue((t.WalletId, t.TxId), out var tags) ? tags : Array.Empty<string>())).ToList();

        return new RecentActivityPageDto(skip, take, total, items);
    }
}
