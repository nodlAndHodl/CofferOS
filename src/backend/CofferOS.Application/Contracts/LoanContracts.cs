namespace CofferOS.Application.Contracts;

/// <summary>Request to create a new loan.</summary>
public sealed record CreateLoanRequest(
    string Name,
    string? Lender,
    decimal PrincipalAmount,
    decimal CurrentBalance,
    decimal InterestRate,
    string InterestType,
    DateTimeOffset LoanStartDate,
    int? LoanTermMonths,
    string PaymentFrequency,
    decimal CollateralAmountBtc,
    decimal CurrentBtcPrice,
    decimal WarningLtv,
    decimal LiquidationLtv,
    decimal? CollateralCostBasis,
    string? Notes,
    string InterestPaymentSchedule = "Accruing",
    string Currency = "USD");

/// <summary>Request to update an existing loan.</summary>
public sealed record UpdateLoanRequest(
    string Name,
    string? Lender,
    decimal PrincipalAmount,
    decimal CurrentBalance,
    decimal InterestRate,
    string InterestType,
    DateTimeOffset LoanStartDate,
    int? LoanTermMonths,
    string PaymentFrequency,
    decimal CollateralAmountBtc,
    decimal CurrentBtcPrice,
    decimal WarningLtv,
    decimal LiquidationLtv,
    decimal? CollateralCostBasis,
    string? Notes,
    string InterestPaymentSchedule = "Accruing",
    string Currency = "USD");

/// <summary>Loan summary for list views.</summary>
public sealed record LoanSummaryDto(
    Guid Id,
    string Name,
    string? Lender,
    string Status,
    decimal PrincipalAmount,
    decimal CurrentBalance,
    decimal InterestRate,
    string InterestType,
    decimal CollateralAmountBtc,
    decimal CurrentBtcPrice,
    decimal CurrentCollateralValue,
    decimal CurrentLtv,
    decimal WarningLtv,
    decimal LiquidationLtv,
    decimal DistanceToWarning,
    decimal DistanceToLiquidation,
    decimal CollateralCostBasis,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Full loan detail with all calculated fields.</summary>
public sealed record LoanDetailDto(
    Guid Id,
    string Name,
    string? Lender,
    string Status,
    string? Notes,
    decimal PrincipalAmount,
    decimal CurrentBalance,
    decimal InterestRate,
    string InterestType,
    DateTimeOffset LoanStartDate,
    int? LoanTermMonths,
    string PaymentFrequency,
    string InterestPaymentSchedule,
    decimal CollateralAmountBtc,
    decimal CurrentBtcPrice,
    decimal CurrentCollateralValue,
    decimal CurrentLtv,
    decimal WarningLtv,
    decimal LiquidationLtv,
    decimal WarningPrice,
    decimal LiquidationPrice,
    decimal DistanceToWarning,
    decimal DistanceToLiquidation,
    decimal RemainingCollateralBuffer,
    decimal CollateralCostBasis,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Treasury dashboard summary.</summary>
public sealed record TreasurySummaryDto(
    int ActiveLoanCount,
    decimal TotalLoanBalance,
    decimal TotalCollateralBtc,
    decimal TotalCollateralValue,
    decimal AverageLtv,
    LoanSummaryDto? HighestRiskLoan,
    decimal? CurrentBtcPrice,
    string PriceProvider);

/// <summary>Historical price snapshot for a loan.</summary>
public sealed record LoanPriceSnapshotDto(
    DateTimeOffset SnapshotDate,
    decimal PriceUsd,
    decimal CurrentBalance,
    decimal CollateralValue,
    decimal Ltv);

/// <summary>Historical price data for a loan with calculated LTV.</summary>
public sealed record LoanHistoricalDataDto(
    Guid LoanId,
    string Currency,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    IReadOnlyList<LoanPriceSnapshotDto> Snapshots);
