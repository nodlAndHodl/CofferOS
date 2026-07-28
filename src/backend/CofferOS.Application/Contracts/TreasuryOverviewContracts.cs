namespace CofferOS.Application.Contracts;

/// <summary>Complete dashboard overview including wallets, holdings, treasury, and activity.</summary>
public sealed record DashboardOverviewDto(
    // Bitcoin Holdings
    decimal TotalBitcoin,
    decimal AvailableBitcoin,
    decimal CollateralBitcoin,
    decimal BitcoinPriceUsd,
    decimal TotalValueUsd,

    // Treasury
    int ActiveLoanCount,
    decimal OutstandingLoanBalanceUsd,
    decimal WeightedAverageLtv,
    LoanSummaryDto? HighestRiskLoan,

    // Wallets / Recent Activity
    int WalletCount,
    BalanceDto TotalBalance,
    IReadOnlyList<WalletSummaryDto> Wallets,
    RecentActivityPageDto RecentActivity,

    // Metadata
    DateTime LastUpdatedUtc);
