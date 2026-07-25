using CofferOS.Domain.Common;

namespace CofferOS.Domain.Events;

/// <summary>
/// Published when a new Bitcoin price has been successfully obtained and persisted.
/// </summary>
public sealed record PriceUpdatedEvent(
    decimal PriceUsd,
    string Provider,
    DateTimeOffset OccurredOn) : IDomainEvent;
