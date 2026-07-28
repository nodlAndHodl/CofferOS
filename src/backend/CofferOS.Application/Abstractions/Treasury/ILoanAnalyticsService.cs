namespace CofferOS.Application.Abstractions.Treasury;

/// <summary>
/// Analyzes loan portfolio for risk metrics and health indicators.
/// </summary>
public interface ILoanAnalyticsService
{
    /// <summary>
    /// Gets the loan with the highest LTV (closest to liquidation).
    /// </summary>
    Task<LoanRiskAnalysis?> GetHighestRiskLoanAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the loan nearest to its warning threshold.
    /// </summary>
    Task<LoanRiskAnalysis?> GetNearestWarningThresholdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets estimated liquidation prices for all active loans.
    /// </summary>
    Task<IReadOnlyList<LoanLiquidationEstimate>> GetLiquidationEstimatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets collateral utilization metrics.
    /// </summary>
    Task<CollateralUtilization> GetCollateralUtilizationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates a risk score (0-100) for the loan portfolio.
    /// Higher score = higher risk.
    /// </summary>
    Task<int> CalculatePortfolioRiskScoreAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Risk analysis for a single loan.
/// </summary>
public sealed record LoanRiskAnalysis(
    Guid LoanId,
    string LoanName,
    decimal CurrentLtv,
    decimal WarningLtv,
    decimal LiquidationLtv,
    decimal DistanceToWarning,
    decimal DistanceToLiquidation,
    decimal WarningPrice,
    decimal LiquidationPrice);

/// <summary>
/// Liquidation price estimate for a loan.
/// </summary>
public sealed record LoanLiquidationEstimate(
    Guid LoanId,
    string LoanName,
    decimal CurrentBtcPrice,
    decimal WarningPrice,
    decimal LiquidationPrice,
    decimal PercentageToWarning,
    decimal PercentageToLiquidation);

/// <summary>
/// Collateral utilization metrics.
/// </summary>
public sealed record CollateralUtilization(
    decimal TotalCollateralBtc,
    decimal TotalCollateralValueUsd,
    decimal TotalLoanBalanceUsd,
    decimal WeightedAverageLtv,
    int ActiveLoansCount,
    decimal AverageCollateralPerLoan);
