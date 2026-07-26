using CofferOS.Domain.Common;

namespace CofferOS.Domain.Treasury;

/// <summary>
/// Historical price snapshot for a loan, used to calculate historical LTV and other metrics.
/// Linked to a specific loan to enable per-loan historical analysis without repeated API calls.
/// </summary>
public sealed class LoanPriceSnapshot
{
    private LoanPriceSnapshot() { }

    public LoanPriceSnapshot(
        Guid loanId,
        DateTimeOffset snapshotDate,
        decimal priceUsd,
        string source = "coingecko")
    {
        if (loanId == Guid.Empty) throw new ArgumentException("LoanId is required.", nameof(loanId));
        if (snapshotDate == default) throw new ArgumentException("Snapshot date is required.", nameof(snapshotDate));
        if (priceUsd < 0) throw new ArgumentException("Price cannot be negative.", nameof(priceUsd));
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Source is required.", nameof(source));

        LoanId = loanId;
        SnapshotDate = snapshotDate;
        PriceUsd = priceUsd;
        Source = source.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid LoanId { get; private set; }
    public DateTimeOffset SnapshotDate { get; private set; }
    public decimal PriceUsd { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
