using CofferOS.Domain.Common;

namespace CofferOS.Domain.Treasury;

/// <summary>
/// Historical price snapshot for a loan, used to calculate historical LTV and other metrics.
/// The PriceUsd value is actually the BTC price in the snapshot's Currency (not necessarily USD).
/// </summary>
public sealed class LoanPriceSnapshot
{
    private LoanPriceSnapshot() { }

    public LoanPriceSnapshot(
        Guid loanId,
        DateTimeOffset snapshotDate,
        decimal price,
        string currency = "USD",
        string source = "coingecko")
    {
        if (loanId == Guid.Empty) throw new ArgumentException("LoanId is required.", nameof(loanId));
        if (snapshotDate == default) throw new ArgumentException("Snapshot date is required.", nameof(snapshotDate));
        if (price < 0) throw new ArgumentException("Price cannot be negative.", nameof(price));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Source is required.", nameof(source));

        LoanId = loanId;
        SnapshotDate = snapshotDate;
        PriceUsd = price;
        Currency = currency.Trim().ToUpperInvariant();
        Source = source.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid LoanId { get; private set; }
    public DateTimeOffset SnapshotDate { get; private set; }
    public decimal PriceUsd { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string Source { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
