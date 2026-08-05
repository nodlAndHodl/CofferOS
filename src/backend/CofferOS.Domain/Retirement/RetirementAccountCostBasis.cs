using CofferOS.Domain.Common;

namespace CofferOS.Domain.Retirement;

/// <summary>
/// A cost basis entry for a retirement account holding.
/// Tracks acquisition date and cost basis separately to support multiple purchases.
/// </summary>
public sealed class RetirementAccountCostBasis : Entity
{
    private RetirementAccountCostBasis() { }

    public RetirementAccountCostBasis(Guid accountId, decimal costBasis, DateTimeOffset acquisitionDate)
    {
        AccountId = accountId;
        CostBasis = costBasis;
        AcquisitionDate = acquisitionDate;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid AccountId { get; private set; }
    public decimal CostBasis { get; private set; }
    public DateTimeOffset AcquisitionDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void UpdateCostBasis(decimal costBasis)
    {
        if (costBasis < 0)
            throw new ArgumentException("Cost basis cannot be negative.", nameof(costBasis));
        CostBasis = costBasis;
    }
}
