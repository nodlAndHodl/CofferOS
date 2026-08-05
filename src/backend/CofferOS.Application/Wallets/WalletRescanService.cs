using System.Linq;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Contracts;
using CofferOS.Domain.Wallets;

namespace CofferOS.Application.Wallets;

/// <summary>Scans the blockchain for UTXOs and transaction history, then persists the results.</summary>
public sealed class WalletRescanService
{
    private readonly IWalletRepository _wallets;
    private readonly IUtxoProvider _utxoProvider;
    private readonly IAddressRepository _addresses;
    private readonly IEnumerable<IWalletHistoryProvider> _historyProviders;
    private readonly IEnumerable<IBitcoinNodeProvider> _nodeProviders;
    private readonly IUnitOfWork _unitOfWork;

    public WalletRescanService(
        IWalletRepository wallets,
        IUtxoProvider utxoProvider,
        IAddressRepository addresses,
        IEnumerable<IWalletHistoryProvider> historyProviders,
        IEnumerable<IBitcoinNodeProvider> nodeProviders,
        IUnitOfWork unitOfWork)
    {
        _wallets = wallets;
        _utxoProvider = utxoProvider;
        _addresses = addresses;
        _historyProviders = historyProviders;
        _nodeProviders = nodeProviders;
        _unitOfWork = unitOfWork;
    }

    public async Task<RescanResultDto> RescanAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var wallet = await _wallets.GetByIdWithDescriptorsAsync(walletId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet not found.");

        var rawDescriptors = wallet.Descriptors.Select(d => d.Raw).ToList();
        var descriptorsWithId = wallet.Descriptors.Select(d => (d.Id, d.Raw, d.ScriptType)).ToList();

        var historyProvider = _historyProviders.FirstOrDefault();
        WalletHistoryScan? history = null;
        IReadOnlyList<NodeUtxo> nodeUtxos;

        if (historyProvider is not null)
        {
            history = await historyProvider.GetWalletHistoryAsync(descriptorsWithId, cancellationToken);
            nodeUtxos = history.Utxos;
        }
        else
        {
            nodeUtxos = await _utxoProvider.ScanUtxoSetAsync(rawDescriptors, cancellationToken);
        }

        var historyTip = historyProvider is not null ? await historyProvider.GetTipHeightAsync(cancellationToken) : null;

        var nodeProvider = _nodeProviders.FirstOrDefault();
        var blockchainInfo = historyTip is null && nodeProvider is not null
            ? await nodeProvider.GetBlockchainInfoAsync(cancellationToken)
            : null;
        var currentHeight = historyTip ?? blockchainInfo?.Blocks;

        int ComputeConfirmations(long? height)
        {
            if (!height.HasValue) return 0;
            if (!currentHeight.HasValue) return 1;
            var confirmations = (int)(currentHeight.Value - height.Value + 1);
            return confirmations < 1 ? 1 : confirmations;
        }

        var utxos = nodeUtxos
            .Select(u => new Utxo(
                walletId,
                u.TxId,
                u.Vout,
                u.ValueSats,
                u.ScriptPubKeyHex,
                u.Address,
                ComputeConfirmations(u.Height),
                u.Height))
            .ToList();

        await _wallets.ReplaceUtxosAsync(walletId, utxos, cancellationToken);

        if (history is not null)
        {
            var transactions = history.Transactions
                .Select(t => new WalletTransaction(
                    walletId,
                    t.TxId,
                    t.NetAmountSats,
                    t.FeeSats,
                    t.Direction,
                    ComputeConfirmations(t.BlockHeight),
                    t.BlockHeight,
                    null,
                    t.Timestamp))
                .ToList();

            static long SortKey(long height) => height > 0 ? height : long.MaxValue;
            var historyByAddress = history.AddressHistory
                .GroupBy(h => h.Address)
                .ToDictionary(
                    g => g.Key,
                    g => (
                        Count: g.Count(),
                        First: g.OrderBy(x => SortKey(x.Height)).First().TxId,
                        Last: g.OrderByDescending(x => SortKey(x.Height)).First().TxId));

            var satsByAddress = utxos
                .Where(u => !u.IsSpent)
                .GroupBy(u => u.Address)
                .ToDictionary(g => g.Key ?? string.Empty, g => g.Sum(x => x.ValueSats));

            var addresses = history.DerivedAddresses
                .Select(a =>
                {
                    var addr = new Address(walletId, a.DescriptorId, a.Index, a.IsChange, a.Address, a.ScriptPubKeyHex);
                    var hasHistory = historyByAddress.TryGetValue(a.Address, out var h);
                    var sats = satsByAddress.TryGetValue(a.Address, out var s) ? s : 0L;
                    addr.UpdateStats(
                        hasHistory ? h.Count : 0,
                        hasHistory ? h.First : null,
                        hasHistory ? h.Last : null,
                        sats);
                    return addr;
                })
                .ToList();

            await _wallets.ReplaceTransactionsAsync(walletId, transactions, cancellationToken);
            await _addresses.ReplaceAddressesAsync(walletId, addresses, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var balance = WalletQueryService.ComputeBalance(utxos);
        return new RescanResultDto(utxos.Count, balance);
    }
}
