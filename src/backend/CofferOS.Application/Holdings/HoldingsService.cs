using CofferOS.Application.CostBasis;
using CofferOS.Application.Abstractions.Holdings;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Contracts;
using CofferOS.Application.Wallets;
using CofferOS.Domain.Common;

namespace CofferOS.Application.Holdings;

/// <summary>
/// Aggregates Bitcoin holdings from all sources.
/// Currently supports wallet-based holdings; extensible for future sources.
/// </summary>
public sealed class HoldingsService : IHoldingsService
{
    private readonly WalletQueryService _walletQueries;
    private readonly ILoanRepository _loans;
    private readonly IRetirementAccountRepository _retirementAccounts;
    private readonly IBitcoinPriceProvider _priceProvider;
    private readonly CostBasisService _costBasis;

    public HoldingsService(
        WalletQueryService walletQueries,
        ILoanRepository loans,
        IRetirementAccountRepository retirementAccounts,
        IBitcoinPriceProvider priceProvider,
        CostBasisService costBasis)
    {
        _walletQueries = walletQueries;
        _loans = loans;
        _retirementAccounts = retirementAccounts;
        _priceProvider = priceProvider;
        _costBasis = costBasis;
    }

    public async Task<HoldingsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var breakdown = await GetBreakdownAsync(cancellationToken);
        var btcPrice = await _priceProvider.GetCurrentPriceAsync(cancellationToken) ?? 0m;
        var totalValue = breakdown.TotalBitcoin * btcPrice;

        var walletSummaries = await _walletQueries.GetSummariesAsync(cancellationToken);
        var activeLoans = await _loans.GetActiveAsync(cancellationToken);
        var retirementAccounts = await _retirementAccounts.GetAllAsync(cancellationToken);

        // Cost basis
        var walletCostBasis = walletSummaries.Sum(w => w.TotalCostBasis);

        var loanIds = activeLoans.Select(l => l.Id.ToString()).ToList();
        var loanCostBasisById = await _costBasis.GetByReferencesAsync(
            CostBasisTarget.LoanCollateral,
            loanIds,
            cancellationToken);
        var collateralCostBasis = loanCostBasisById.Values.Sum();

        var retirementCostBasis = retirementAccounts.Sum(a => a.GetTotalCostBasis());

        var totalCostBasis = walletCostBasis + collateralCostBasis + retirementCostBasis;
        var unrealizedPnl = totalValue - totalCostBasis;
        var unrealizedPnlPercent = totalCostBasis > 0 ? unrealizedPnl / totalCostBasis : 0m;

        var categories = new List<HoldingBreakdownDto>();

        decimal walletBtc = 0m;
        foreach (var w in walletSummaries)
            walletBtc += w.Balance.TotalBtc;

        decimal collateralBtc = breakdown.CollateralBitcoin;

        decimal retirementBtc = retirementAccounts.Sum(a => a.BitcoinAmount);

        if (walletBtc > 0)
        {
            var walletValue = walletBtc * btcPrice;
            categories.Add(new HoldingBreakdownDto
            {
                Category = "Wallet Holdings",
                BitcoinAmount = walletBtc,
                Percentage = breakdown.TotalBitcoin > 0 ? walletBtc / breakdown.TotalBitcoin : 0,
                Value = walletValue,
                CostBasis = walletCostBasis,
                UnrealizedPnl = walletValue - walletCostBasis,
                Count = walletSummaries.Count
            });
        }

        if (collateralBtc > 0)
        {
            var collateralValue = collateralBtc * btcPrice;
            categories.Add(new HoldingBreakdownDto
            {
                Category = "Collateral",
                BitcoinAmount = collateralBtc,
                Percentage = breakdown.TotalBitcoin > 0 ? collateralBtc / breakdown.TotalBitcoin : 0,
                Value = collateralValue,
                CostBasis = collateralCostBasis,
                UnrealizedPnl = collateralValue - collateralCostBasis,
                Count = activeLoans.Count
            });
        }

        if (retirementBtc > 0)
        {
            var retirementValue = retirementBtc * btcPrice;
            categories.Add(new HoldingBreakdownDto
            {
                Category = "Retirement Accounts",
                BitcoinAmount = retirementBtc,
                Percentage = breakdown.TotalBitcoin > 0 ? retirementBtc / breakdown.TotalBitcoin : 0,
                Value = retirementValue,
                CostBasis = retirementCostBasis,
                UnrealizedPnl = retirementValue - retirementCostBasis,
                Count = retirementAccounts.Count
            });
        }

        return new HoldingsSummaryDto
        {
            TotalBitcoin = breakdown.TotalBitcoin,
            AvailableBitcoin = breakdown.AvailableBitcoin,
            CollateralBitcoin = breakdown.CollateralBitcoin,
            TotalValue = totalValue,
            TotalCostBasis = totalCostBasis,
            UnrealizedPnl = unrealizedPnl,
            UnrealizedPnlPercent = unrealizedPnlPercent,
            Breakdown = categories
        };
    }

    public async Task<IReadOnlyList<HoldingDto>> GetHoldingsAsync(CancellationToken cancellationToken = default)
    {
        var btcPrice = await _priceProvider.GetCurrentPriceAsync(cancellationToken) ?? 0m;
        var walletSummaries = await _walletQueries.GetSummariesAsync(cancellationToken);
        var activeLoans = await _loans.GetActiveAsync(cancellationToken);
        var retirementAccounts = await _retirementAccounts.GetAllAsync(cancellationToken);

        var holdings = new List<HoldingDto>();

        foreach (var wallet in walletSummaries)
        {
            var value = wallet.Balance.TotalBtc * btcPrice;
            holdings.Add(new HoldingDto
            {
                Id = wallet.Id,
                Type = HoldingType.Wallet,
                Name = wallet.Name,
                BitcoinAmount = wallet.Balance.TotalBtc,
                AvailableBitcoin = wallet.Balance.TotalBtc,
                LockedBitcoin = 0m,
                Value = value,
                CostBasis = wallet.TotalCostBasis,
                UnrealizedPnl = value - wallet.TotalCostBasis,
                IsReadOnly = true,
                Institution = null
            });
        }

        var loanIds = activeLoans.Select(l => l.Id.ToString()).ToList();
        var costBasisByLoan = await _costBasis.GetByReferencesAsync(
            CostBasisTarget.LoanCollateral,
            loanIds,
            cancellationToken);

        foreach (var loan in activeLoans)
        {
            var value = loan.CollateralAmountBtc * btcPrice;
            var costBasis = costBasisByLoan.GetValueOrDefault(loan.Id.ToString());
            holdings.Add(new HoldingDto
            {
                Id = loan.Id,
                Type = HoldingType.LoanCollateral,
                Name = loan.Name,
                BitcoinAmount = loan.CollateralAmountBtc,
                AvailableBitcoin = 0m,
                LockedBitcoin = loan.CollateralAmountBtc,
                Value = value,
                CostBasis = costBasis,
                UnrealizedPnl = value - costBasis,
                IsReadOnly = true,
                Institution = loan.Lender
            });
        }

        foreach (var account in retirementAccounts)
        {
            var value = account.BitcoinAmount * btcPrice;
            var costBasis = account.GetTotalCostBasis();
            holdings.Add(new HoldingDto
            {
                Id = account.Id,
                Type = HoldingType.Retirement,
                Name = account.Name,
                BitcoinAmount = account.BitcoinAmount,
                AvailableBitcoin = account.BitcoinAmount,
                LockedBitcoin = 0m,
                Value = value,
                CostBasis = costBasis,
                UnrealizedPnl = value - costBasis,
                IsReadOnly = false,
                Institution = account.Provider
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

        decimal totalRetirementBtc = 0m;
        var retirementAccounts = await _retirementAccounts.GetAllAsync(cancellationToken);
        foreach (var account in retirementAccounts)
        {
            totalRetirementBtc += account.BitcoinAmount;
        }

        // Total = wallets + collateral + retirement
        // Available = wallet balance + retirement (both fully available, not reduced by collateral)
        decimal totalBtc = totalWalletBtc + totalCollateralBtc + totalRetirementBtc;

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
                CollateralBitcoin: totalCollateralBtc),
            new HoldingSource(
                SourceType: "Retirement",
                DisplayName: "Retirement Accounts",
                TotalBitcoin: totalRetirementBtc,
                AvailableBitcoin: totalRetirementBtc,
                CollateralBitcoin: 0m)
        };

        return new HoldingsBreakdown(
            TotalBitcoin: totalBtc,
            AvailableBitcoin: totalWalletBtc + totalRetirementBtc,
            CollateralBitcoin: totalCollateralBtc,
            Sources: sources);
    }
}
