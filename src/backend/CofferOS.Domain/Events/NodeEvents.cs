using CofferOS.Domain.Common;

namespace CofferOS.Domain.Events;

/// <summary>
/// Raised when a connected Bitcoin node reports a new chain tip. This is the
/// entry point of the observability pipeline: New Block -> Wallet Scanner ->
/// Transaction Updated -> Dashboard Updated.
/// </summary>
public sealed record NewBlockDetectedEvent(BitcoinNetwork Network, int Height, string BlockHash) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
