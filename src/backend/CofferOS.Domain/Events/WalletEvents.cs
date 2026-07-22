using CofferOS.Domain.Common;

namespace CofferOS.Domain.Events;

/// <summary>Raised when a new watch-only wallet is imported into CofferOS.</summary>
public sealed record WalletImportedEvent(Guid WalletId, string Name) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Raised when a descriptor / xpub is added to a wallet.</summary>
public sealed record DescriptorAddedEvent(Guid WalletId, Guid DescriptorId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Raised when a new address is derived from a descriptor.</summary>
public sealed record AddressGeneratedEvent(Guid WalletId, Guid DescriptorId, string Address, int DerivationIndex) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Raised when a wallet transaction is discovered or its confirmation state changes.</summary>
public sealed record TransactionUpdatedEvent(Guid WalletId, string TxId, int Confirmations) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
