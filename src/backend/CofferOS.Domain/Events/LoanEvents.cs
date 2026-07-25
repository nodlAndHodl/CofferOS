using CofferOS.Domain.Common;

namespace CofferOS.Domain.Events;

/// <summary>Domain event raised when a loan's calculated state has been updated (e.g. by daily accrual).</summary>
public sealed record LoanUpdatedEvent(Guid LoanId, DateTimeOffset OccurredOn) : IDomainEvent;
