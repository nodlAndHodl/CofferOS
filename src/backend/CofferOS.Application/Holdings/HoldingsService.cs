using CofferOS.Application.Abstractions.Holdings;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Contracts;
using CofferOS.Application.Wallets;

namespace CofferOS.Application.Holdings;

/// <summary>
/// Aggregates Bitcoin holdings from all sources.
/// Currently supports wallet-based holdings; extensible for future sources.
/// </summary>
public sealed class HoldingsService : IHoldingsService
{
    private readonly WalletQueryService _walletQueries;
    private readonly ILoanRepository _loans;
    private readonly IBitcoinPriceProvider _priceProvider;

    public HoldingsService(
        WalletQueryService walletQueries,
        ILoanRepository loans,
        IBitcoinPriceProvider priceProvider)
    {
        _walletQueries = walletQueries;
        _loans = loans;
        _priceProvider = priceProvider;
    }

    public async Task<HoldingsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var breakdown = await GetBreakdownAsync(cancellationToken);
        var btcPrice = await _priceProvider.GetCurrentPriceAsync(cancellationToken) ?? 0m;

        var walletSummaries = await _walletQueries.GetSummariesAsync(cancellationToken);
        var activeLoans = await _loans.GetActiveAsync(cancellationToken);

        var categories = new List<HoldingBreakdownDto>();

        decimal walletBtc = 0m;
        foreach (var w in walletSummaries)
            walletBtc += w.Balance.TotalBtc;

        decimal collateralBtc = breakdown.CollateralBitcoin;

        if (walletBtc > 0)
        {
            categories.Add(new HoldingBreakdownDto
            {
                Category = "Wallet Holdings",
                BitcoinAmount = walletBtc,
                Percentage = breakdown.TotalBitcoin > 0 ? walletBtc / breakdown.TotalBitcoin : 0,
                ValueUsd = walletBtc * btcPrice,
                Count = walletSummaries.Count
            });
        }

        if (collateralBtc > 0)
        {
            categories.Add(new HoldingBreakdownDto
            {
                Category = "Collateral",
                BitcoinAmount = collateralBtc,
                Percentage = breakdown.TotalBitcoin > 0 ? collateralBtc / breakdown.TotalBitcoin : 0,
                ValueUsd = collateralBtc * btcPrice,
                Count = activeLoans.Count
            });
        }

        return new HoldingsSummaryDto
        {
            TotalBitcoin = breakdown.TotalBitcoin,
            AvailableBitcoin = breakdown.AvailableBitcoin,
            CollateralBitcoin = breakdown.CollateralBitcoin,
            TotalValueUsd = breakdown.TotalBitcoin * btcPrice,
            Breakdown = categories
        };
    }

    public async Task<IReadOnlyList<HoldingDto>> GetHoldingsAsync(CancellationToken cancellationToken = default)
    {
        var btcPrice = await _priceProvider.GetCurrentPriceAsync(cancellationToken) ?? 0m;
        var walletSummaries = await _walletQueries.GetSummariesAsync(cancellationToken);
        var activeLoans = await _loans.GetActiveAsync(cancellationToken);

        var holdings = new List<HoldingDto>();

        foreach (var wallet in walletSummaries)
        {
            holdings.Add(new HoldingDto
            {
                Id = wallet.Id,
                Type = HoldingType.Wallet,
                Name = wallet.Name,
                BitcoinAmount = wallet.Balance.TotalBtc,
                AvailableBitcoin = wallet.Balance.TotalBtc,
                LockedBitcoin = 0m,
                ValueUsd = wallet.Balance.TotalBtc * btcPrice,
                IsReadOnly = true,
                Institution = null
            });
        }

        foreach (var loan in activeLoans)
        {
            holdings.Add(new HoldingDto
            {
                Id = loan.Id,
                Type = HoldingType.LoanCollateral,
                Name = loan.Name,
                BitcoinAmount = loan.CollateralAmountBtc,
                AvailableBitcoin = 0m,
                LockedBitcoin = loan.CollateralAmountBtc,
                ValueUsd = loan.CollateralAmountBtc * btcPrice,
                IsReadOnly = true,
                Institution = loan.Lender
            });
        }

        return holdings;
    }

    public async Task<decimal> GetTotalBitcoinAsync(CancellationToken cancellationToken = default)
    {
        var breakdown = await GetBreakdownAsync(cancellationToken);
        return breakdown.TotalBitcoin;
    }

    public async Task<decimal> GetAvailableBitcoinAsync(CancellationToken cancellationToken = default)
    {
        var breakdown = await GetBreakdownAsync(cancellationToken);
        return breakdown.AvailableBitcoin;
    }

    public async Task<decimal> GetCollateralBitcoinAsync(CancellationToken cancellationToken = default)
    {
        var breakdown = await GetBreakdownAsync(cancellationToken);
        return breakdown.CollateralBitcoin;
    }

    public async Task<HoldingsBreakdown> GetBreakdownAsync(CancellationToken cancellationToken = default)
    {
        var walletSummaries = await _walletQueries.GetSummariesAsync(cancellationToken);
        
        decimal totalWalletBtc = 0m;
        foreach (var wallet in walletSummaries)
        {
            totalWalletBtc += wallet.Balance.TotalBtc;
        }

        decimal totalCollateralBtc = 0m;
        var activeLoans = await _loans.GetActiveAsync(cancellationToken);
        foreach (var loan in activeLoans)
        {
            totalCollateralBtc += loan.CollateralAmountBtc;
        }

        // Total = wallets + collateral (collateral is separate Bitcoin held at lenders)
        // Available = wallet balance (fully available, not reduced by collateral)
        decimal totalBtc = totalWalletBtc + totalCollateralBtc;

        var sources = new List<HoldingSource>
        {
            new HoldingSource(
                SourceType: "Wallets",
                DisplayName: "Self-Custody Wallets",
                TotalBitcoin: totalWalletBtc,
                AvailableBitcoin: totalWalletBtc,
                CollateralBitcoin: 0m),
            new HoldingSource(
                SourceType: "Collateral",
                DisplayName: "Loan Collateral",
                TotalBitcoin: totalCollateralBtc,
                AvailableBitcoin: 0m,
                CollateralBitcoin: totalCollateralBtc)
        };

        return new HoldingsBreakdown(
            TotalBitcoin: totalBtc,
            AvailableBitcoin: totalWalletBtc,
            CollateralBitcoin: totalCollateralBtc,
            Sources: sources);
    }
}
