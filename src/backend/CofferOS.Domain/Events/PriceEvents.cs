using CofferOS.Domain.Common;

namespace CofferOS.Domain.Events;

/// <summary>
/// Published when a new Bitcoin price has been successfully obtained and persisted.
/// ExchangeRates contains BTC price keyed by lowercase ISO-4217 code (e.g. "usd", "eur").
/// </summary>
public sealed record PriceUpdatedEvent(
    decimal PriceUsd,
    string Provider,
    DateTimeOffset OccurredOn,
    IReadOnlyDictionary<string, decimal>? ExchangeRates = null) : IDomainEvent;
