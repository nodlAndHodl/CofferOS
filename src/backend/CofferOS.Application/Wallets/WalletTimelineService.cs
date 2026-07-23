using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Contracts;
using CofferOS.Domain.Common;
using CofferOS.Domain.Wallets;

namespace CofferOS.Application.Wallets;

/// <summary>
/// Builds a chronological timeline of a wallet's life by merging stored user
/// annotations with events generated from on-chain transaction history. Future
/// event sources (Lightning, nodes, multisig, migrations) are modelled through
/// <see cref="TimelineEventType"/> and can be persisted as <see cref="TimelineEvent"/>.
/// </summary>
public sealed class WalletTimelineService
{
    private readonly IWalletRepository _wallets;
    private readonly IWalletReadStore _readStore;
    private readonly ITimelineEventRepository _timelineEvents;

    public WalletTimelineService(IWalletRepository wallets, IWalletReadStore readStore, ITimelineEventRepository timelineEvents)
    {
        _wallets = wallets;
        _readStore = readStore;
        _timelineEvents = timelineEvents;
    }

    public async Task<WalletTimelineDto> GetTimelineAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var wallet = await _wallets.GetByIdAsync(walletId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet not found.");

        var transactions = await _readStore.GetTransactionsAsync(walletId, cancellationToken);
        var utxos = await _readStore.GetUtxosAsync(walletId, cancellationToken);
        var stored = await _timelineEvents.GetByWalletAsync(walletId, cancellationToken);

        var entries = new List<TimelineEntryDto>();

        entries.Add(new TimelineEntryDto(
            null,
            TimelineEventType.WalletImported.ToString(),
            wallet.CreatedAt,
            "Wallet imported",
            $"Imported {wallet.Name} into CofferOS",
            null,
            null,
            null,
            false));

        foreach (var tx in transactions)
        {
            var type = tx.Direction switch
            {
                TransactionDirection.Incoming => TimelineEventType.TransactionReceived,
                TransactionDirection.Outgoing => TimelineEventType.TransactionSent,
                _ => TimelineEventType.TransactionReceived
            };

            var verb = tx.Direction == TransactionDirection.Outgoing ? "Sent" : "Received";
            var title = $"{verb} {Math.Abs(tx.NetAmountSats).SatsToBtc():F8} BTC";
            var description = tx.Direction == TransactionDirection.Outgoing
                ? $"Outgoing transaction with {tx.FeeSats} sats fee"
                : "Incoming on-chain transaction";

            entries.Add(new TimelineEntryDto(
                null,
                type.ToString(),
                tx.Timestamp ?? wallet.CreatedAt,
                title,
                description,
                tx.TxId,
                tx.NetAmountSats,
                null,
                false));
        }

        foreach (var e in stored)
        {
            entries.Add(new TimelineEntryDto(
                e.Id,
                e.Type.ToString(),
                e.OccurredAt,
                e.Title,
                e.Description,
                e.Reference,
                null,
                null,
                true));
        }

        // Sort chronologically, then compute the running balance.
        var ordered = entries.OrderBy(e => e.OccurredAt).ToList();
        var withBalance = new List<TimelineEntryDto>(ordered.Count);
        long running = 0;
        foreach (var entry in ordered)
        {
            if (entry.AmountSats.HasValue)
                running += entry.AmountSats.Value;

            withBalance.Add(entry with { RunningBalanceSats = running });
        }
        entries = withBalance;

        var currentBalance = WalletQueryService.ComputeBalance(utxos);

        // Always end with the current holding state.
        entries.Add(new TimelineEntryDto(
            null,
            "CurrentHoldings",
            DateTimeOffset.UtcNow,
            "Current holdings",
            $"{wallet.Name} current balance",
            null,
            currentBalance.TotalSats,
            currentBalance.TotalSats,
            false));

        return new WalletTimelineDto(walletId, wallet.Name, currentBalance, entries);
    }
}

internal static class WalletTimelineSatsHelpers
{
    public static decimal SatsToBtc(this long sats) => (decimal)sats / 100_000_000m;
}
