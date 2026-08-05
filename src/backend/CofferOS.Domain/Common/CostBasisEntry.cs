using CofferOS.Domain.Wallets;

namespace CofferOS.Domain.Common;

/// <summary>
/// User-provided cost basis (total fiat paid) for a Bitcoin lot. Stored in the
/// display currency of the app (USD by default today) without a currency column
/// so a future settings feature can set the display currency globally.
/// </summary>
public sealed class CostBasisEntry : Entity
{
    private CostBasisEntry() { }

    public CostBasisEntry(CostBasisTarget target, string reference, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Reference is required.", nameof(reference));
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        Target = target;
        Reference = reference.Trim();
        Amount = amount;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public CostBasisTarget Target { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateAmount(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        Amount = amount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
