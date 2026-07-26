namespace CofferOS.Application.Abstractions.Notifications;

/// <summary>Service for notifying clients about loan-related events.</summary>
public interface ILoanNotificationService
{
    Task NotifyLoanCreatedAsync(Guid loanId, string loanName, CancellationToken cancellationToken = default);
    Task NotifyLoanUpdatedAsync(Guid loanId, string loanName, CancellationToken cancellationToken = default);
    Task NotifyLoanDeletedAsync(Guid loanId, CancellationToken cancellationToken = default);
    Task NotifyLoanPaymentRecordedAsync(Guid loanId, decimal amount, CancellationToken cancellationToken = default);
    Task NotifyLoanLiquidationWarningAsync(Guid loanId, decimal currentLtv, decimal warningLtv, CancellationToken cancellationToken = default);
}
