using CofferOS.Application.Contracts;

namespace CofferOS.Application.Abstractions.Holdings;

/// <summary>
/// Aggregates Bitcoin holdings from all sources.
/// Currently supports wallet-based holdings; designed to support future sources
/// like multisig wallets, Lightning, IRAs, ETFs, and brokerage accounts.
/// </summary>
public interface IHoldingsService
{
    /// <summary>
    /// Gets a full holdings summary including breakdown by category.
    /// </summary>
    Task<HoldingsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all individual holdings as a flat list.
    /// </summary>
    Task<IReadOnlyList<HoldingDto>> GetHoldingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total Bitcoin owned across all holdings sources.
    /// </summary>
    Task<decimal> GetTotalBitcoinAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the Bitcoin available for use (not pledged as collateral).
    /// </summary>
    Task<decimal> GetAvailableBitcoinAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the Bitcoin pledged as collateral against loans.
    /// </summary>
    Task<decimal> GetCollateralBitcoinAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a breakdown of holdings by source.
    /// </summary>
    Task<HoldingsBreakdown> GetBreakdownAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Breakdown of Bitcoin holdings by source.
/// Designed to support future holding types without redesign.
/// </summary>
public sealed record HoldingsBreakdown(
    decimal TotalBitcoin,
    decimal AvailableBitcoin,
    decimal CollateralBitcoin,
    IReadOnlyList<HoldingSource> Sources);

/// <summary>
/// A single source of Bitcoin holdings.
/// </summary>
public sealed record HoldingSource(
    string SourceType,
    string DisplayName,
    decimal TotalBitcoin,
    decimal AvailableBitcoin,
    decimal CollateralBitcoin);
