using CofferOS.Application.Abstractions.Descriptors;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Contracts;
using CofferOS.Domain.Common;
using CofferOS.Domain.Wallets;
using Microsoft.Extensions.Logging;

namespace CofferOS.Application.Wallets;

/// <summary>
/// Imports a watch-only wallet: parses the supplied xpub/descriptor, creates the
/// Wallet aggregate, adds the descriptor and pre-derives an initial batch of
/// receive addresses so the UI has something to show immediately.
/// </summary>
public sealed class WalletImportService
{
    private readonly IWalletRepository _wallets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDescriptorParser _parser;
    private readonly IWalletReadStore _readStore;
    private readonly ILogger<WalletImportService> _logger;

    public WalletImportService(
        IWalletRepository wallets,
        IUnitOfWork unitOfWork,
        IDescriptorParser parser,
        IWalletReadStore readStore,
        ILogger<WalletImportService> logger)
    {
        _wallets = wallets;
        _unitOfWork = unitOfWork;
        _parser = parser;
        _readStore = readStore;
        _logger = logger;
    }

    public async Task<WalletSummaryDto> ImportAsync(ImportWalletRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Descriptor))
            throw new ArgumentException("A descriptor or xpub is required.", nameof(request));

        var network = ParseNetwork(request.Network);

        // Parse first so we fail before creating anything if the input is invalid.
        var parsed = _parser.Parse(request.Descriptor.Trim(), network);

        if (!string.IsNullOrWhiteSpace(request.ScriptTypeOverride) &&
            Enum.TryParse<ScriptType>(request.ScriptTypeOverride, ignoreCase: true, out var overrideType) &&
            overrideType != ScriptType.Unknown)
        {
            parsed = parsed with { ScriptType = overrideType };
        }

        var wallet = Wallet.Create(request.Name, request.Description, network);
        var cosignerEntities = parsed.Cosigners
            .Select((c, i) => new Cosigner(i, c.MasterFingerprint, c.OriginPath, c.KeyExpression))
            .ToList();
        var descriptor = wallet.AddDescriptor(
            parsed.Source,
            parsed.ScriptType,
            parsed.Raw,
            parsed.MasterFingerprint,
            parsed.DerivationPath,
            parsed.Checksum,
            parsed.Threshold,
            parsed.IsSortedMulti,
            cosignerEntities);

        var count = Math.Clamp(request.InitialAddressCount, 0, 200);
        if (count > 0)
        {
            var derived = _parser.Derive(parsed, network, change: false, startIndex: 0, count: count);
            foreach (var d in derived)
                descriptor.DeriveAddress(d.Index, d.IsChange, d.Address, d.ScriptPubKeyHex);
        }

        await _wallets.AddAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // The WalletImportedEvent handler has already rescanned and persisted UTXOs/transactions.
        var utxos = await _readStore.GetUtxosAsync(wallet.Id, cancellationToken);
        var txs = await _readStore.GetTransactionsAsync(wallet.Id, cancellationToken);
        var balance = WalletQueryService.ComputeBalance(utxos);

        _logger.LogInformation("Imported watch-only wallet {WalletId} ({Name}) with {AddressCount} derived addresses",
            wallet.Id, wallet.Name, count);

        return new WalletSummaryDto(
            wallet.Id,
            wallet.Name,
            wallet.Description,
            wallet.Network.ToString(),
            wallet.WatchOnly,
            wallet.Descriptors.Count,
            txs.Count,
            balance,
            0m,
            wallet.CreatedAt);
    }

    private static BitcoinNetwork ParseNetwork(string network) =>
        Enum.TryParse<BitcoinNetwork>(network, ignoreCase: true, out var parsed)
            ? parsed
            : BitcoinNetwork.Mainnet;
}
