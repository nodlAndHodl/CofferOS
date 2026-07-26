using CofferOS.Application.Abstractions.Notifications;

namespace CofferOS.Api.WebSockets;

/// <summary>Implementation of loan notifications using generic notification service.</summary>
public sealed class LoanNotificationService : ILoanNotificationService
{
    private readonly INotificationService _notificationService;

    public LoanNotificationService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public Task NotifyLoanCreatedAsync(Guid loanId, string loanName, CancellationToken cancellationToken = default)
    {
        return _notificationService.BroadcastAsync("loan_created", new { loanId, loanName }, cancellationToken);
    }

    public Task NotifyLoanUpdatedAsync(Guid loanId, string loanName, CancellationToken cancellationToken = default)
    {
        return _notificationService.BroadcastAsync("loan_updated", new { loanId, loanName }, cancellationToken);
    }

    public Task NotifyLoanDeletedAsync(Guid loanId, CancellationToken cancellationToken = default)
    {
        return _notificationService.BroadcastAsync("loan_deleted", new { loanId }, cancellationToken);
    }

    public Task NotifyLoanPaymentRecordedAsync(Guid loanId, decimal amount, CancellationToken cancellationToken = default)
    {
        return _notificationService.BroadcastAsync("loan_payment_recorded", new { loanId, amount }, cancellationToken);
    }

    public Task NotifyLoanLiquidationWarningAsync(Guid loanId, decimal currentLtv, decimal warningLtv, CancellationToken cancellationToken = default)
    {
        return _notificationService.BroadcastAsync("loan_liquidation_warning", new { loanId, currentLtv, warningLtv }, cancellationToken);
    }
}
