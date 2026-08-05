using CofferOS.Application.Contracts;

namespace CofferOS.Application.Abstractions.Dashboard;

/// <summary>
/// Orchestrates the assembly of the complete dashboard overview.
/// Single point of entry for the frontend to retrieve dashboard data.
/// </summary>
public interface IDashboardQueryService
{
    /// <summary>
    /// Gets the complete dashboard overview including holdings, treasury metrics, wallets, and recent activity.
    /// </summary>
    Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}
