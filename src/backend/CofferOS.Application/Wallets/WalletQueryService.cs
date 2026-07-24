using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Contracts;
using CofferOS.Domain.Common;
using CofferOS.Domain.Wallets;

namespace CofferOS.Application.Wallets;

/// <summary>Read-side service that maps aggregates + observed data into API DTOs.</summary>
public sealed class WalletQueryService
{
    private readonly IWalletRepository _wallets;
    private readonly IWalletReadStore _readStore;

    public WalletQueryService(IWalletRepository wallets, IWalletReadStore readStore)
    {
        _wallets = wallets;
        _readStore = readStore;
    }

    public async Task<IReadOnlyList<WalletSummaryDto>> GetSummariesAsync(CancellationToken cancellationToken = default)
    {
        var wallets = await _wallets.GetAllAsync(cancellationToken);
        var result = new List<WalletSummaryDto>(wallets.Count);
        foreach (var wallet in wallets)
        {
            var utxos = await _readStore.GetUtxosAsync(wallet.Id, cancellationToken);
            var txs = await _readStore.GetTransactionsAsync(wallet.Id, cancellationToken);
            result.Add(new WalletSummaryDto(
                wallet.Id,
                wallet.Name,
                wallet.Description,
                wallet.Network.ToString(),
                wallet.WatchOnly,
                wallet.Descriptors.Count,
                txs.Count,
                ComputeBalance(utxos),
                wallet.CreatedAt));
        }

        return result;
    }

    public async Task<WalletDetailDto?> GetDetailAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var wallet = await _wallets.GetByIdWithDescriptorsAsync(walletId, cancellationToken);
        if (wallet is null)
            return null;

        var addresses = await _readStore.GetAddressesAsync(walletId, cancellationToken);
        var transactions = await _readStore.GetTransactionsAsync(walletId, cancellationToken);
        var utxos = await _readStore.GetUtxosAsync(walletId, cancellationToken);
        var txTimestamps = transactions.ToDictionary(
            t => t.TxId,
            t => t.Timestamp,
            StringComparer.OrdinalIgnoreCase);
        var labels = await _readStore.GetLabelsAsync(walletId, cancellationToken);
        var notes = await _readStore.GetNotesAsync(walletId, cancellationToken);
        var tags = await _readStore.GetTagsAsync(walletId, cancellationToken);
        var categories = await _readStore.GetCategoriesAsync(walletId, cancellationToken);
        var metadata = await _readStore.GetMetadataAsync(walletId, cancellationToken);

        return new WalletDetailDto(
            wallet.Id,
            wallet.Name,
            wallet.Description,
            wallet.Network.ToString(),
            wallet.WatchOnly,
            ComputeBalance(utxos),
            wallet.Descriptors.Select(d => new DescriptorDto(
                d.Id,
                d.Source.ToString(),
                d.ScriptType.ToString(),
                d.Raw,
                d.MasterFingerprint,
                d.DerivationPath,
                d.Checksum,
                d.Addresses.Count)).ToList(),
            addresses.Select(ToDto).ToList(),
            transactions.Select(ToDto).ToList(),
            utxos.Select(u => ToDto(u, txTimestamps.GetValueOrDefault(u.TxId))).ToList(),
            labels.Select(l => new LabelDto(l.Target.ToString(), l.Reference, l.Text)).ToList(),
            notes.Select(n => new NoteDto(n.Id, n.Target.ToString(), n.Reference, n.Content, n.CreatedAt, n.UpdatedAt)).ToList(),
            tags.Select(t => new TagDto(t.Target.ToString(), t.Reference, t.Value)).ToList(),
            categories.Select(c => new CategoryDto(c.Target.ToString(), c.Reference, c.Name)).ToList(),
            metadata.Select(m => new MetadataEntryDto(m.Target.ToString(), m.Reference, m.Key, m.Value)).ToList(),
            wallet.CreatedAt);
    }

    public static BalanceDto ComputeBalance(IReadOnlyList<Utxo> utxos)
    {
        long confirmed = 0, unconfirmed = 0;
        foreach (var utxo in utxos)
        {
            if (utxo.IsSpent) continue;
            if (utxo.Confirmations > 0) confirmed += utxo.ValueSats;
            else unconfirmed += utxo.ValueSats;
        }

        var total = confirmed + unconfirmed;
        return new BalanceDto(confirmed, unconfirmed, total, (decimal)total / 100_000_000m);
    }

    private static AddressDto ToDto(Address a) =>
        new(a.Id, a.DerivationIndex, a.IsChange, a.Value, a.IsUsed, a.UseCount, a.FirstTxId, a.LastTxId, a.CurrentSats);

    private static TransactionDto ToDto(WalletTransaction t) =>
        new(t.TxId, t.NetAmountSats, t.FeeSats, t.Direction.ToString(), t.Confirmations, t.BlockHeight, t.Timestamp);

    private static UtxoDto ToDto(Utxo u, DateTimeOffset? timestamp) =>
        new(u.TxId, u.Vout, u.ValueSats, u.Address, u.Confirmations, timestamp, u.IsSpent);
}
