using System.Text.Json.Serialization;

namespace CofferOS.Application.Contracts;

/// <summary>Type of Bitcoin holding source.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HoldingType
{
    Wallet,
    LoanCollateral,
    Lightning,
    Retirement,
    Etf,
    Mining,
    Manual
}

/// <summary>Summary of all Bitcoin holdings across all sources.</summary>
public sealed class HoldingsSummaryDto
{
    public decimal TotalBitcoin { get; init; }
    public decimal AvailableBitcoin { get; init; }
    public decimal CollateralBitcoin { get; init; }
    public decimal TotalValue { get; init; }
    public decimal TotalCostBasis { get; init; }
    public decimal UnrealizedPnl { get; init; }
    public decimal UnrealizedPnlPercent { get; init; }
    public IReadOnlyList<HoldingBreakdownDto> Breakdown { get; init; } = [];
}

/// <summary>Breakdown of holdings for a single category.</summary>
public sealed class HoldingBreakdownDto
{
    public string Category { get; init; } = string.Empty;
    public decimal BitcoinAmount { get; init; }
    public decimal Percentage { get; init; }
    public decimal Value { get; init; }
    public decimal CostBasis { get; init; }
    public decimal UnrealizedPnl { get; init; }
    public int Count { get; init; }
}

/// <summary>A single holding entry (wallet, loan collateral, etc.).</summary>
public sealed class HoldingDto
{
    public Guid Id { get; init; }
    public HoldingType Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal BitcoinAmount { get; init; }
    public decimal AvailableBitcoin { get; init; }
    public decimal LockedBitcoin { get; init; }
    public decimal Value { get; init; }
    public decimal CostBasis { get; init; }
    public decimal UnrealizedPnl { get; init; }
    public bool IsReadOnly { get; init; }
    public string? Institution { get; init; }
}
