using CofferOS.Domain.Common;

namespace CofferOS.Domain.Wallets;

/// <summary>
/// A user-recorded moment in a wallet's history ("Moved funds to cold storage",
/// "Opened Lightning node"). Stored events are merged with events generated from
/// on-chain transaction history to build the wallet timeline. The event type is
/// deliberately open-ended so future sources (Lightning, node, multisig,
/// migrations) can write their own entries.
/// </summary>
public sealed class TimelineEvent : Entity
{
    private TimelineEvent() { }

    public TimelineEvent(
        Guid walletId,
        TimelineEventType type,
        DateTimeOffset occurredAt,
        string title,
        string? description = null,
        string? reference = null)
    {
        WalletId = walletId;
        Type = type;
        OccurredAt = occurredAt;
        Title = title;
        Description = description;
        Reference = reference;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid WalletId { get; private set; }
    public TimelineEventType Type { get; private set; }

    /// <summary>When the event actually happened (user supplied, may predate CofferOS).</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>Optional pointer to a related object (txid, node alias, channel id...).</summary>
    public string? Reference { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(DateTimeOffset occurredAt, string title, string? description, string? reference)
    {
        OccurredAt = occurredAt;
        Title = title;
        Description = description;
        Reference = reference;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
