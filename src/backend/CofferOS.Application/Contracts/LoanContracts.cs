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
    string? Notes);

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
    string? Notes);

/// <summary>Request to update just the balance (repayment or draw).</summary>
public sealed record UpdateLoanBalanceRequest(decimal CurrentBalance);

/// <summary>Request to update collateral and/or price.</summary>
public sealed record UpdateLoanCollateralRequest(decimal CollateralAmountBtc, decimal CurrentBtcPrice);

/// <summary>Request to set the BTC price manually.</summary>
public sealed record SetBtcPriceRequest(decimal Price);

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
