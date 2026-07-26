using CofferOS.Application.Abstractions.Notifications;

namespace CofferOS.Api.WebSockets;

/// <summary>Implementation of wallet notifications using generic notification service.</summary>
public sealed class WalletNotificationService : IWalletNotificationService
{
    private readonly INotificationService _notificationService;

    public WalletNotificationService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public Task NotifyWalletImportedAsync(Guid walletId, string walletName, CancellationToken cancellationToken = default)
    {
        return _notificationService.BroadcastAsync("wallet_imported", new { walletId, walletName }, cancellationToken);
    }

    public Task NotifyWalletRescanStartedAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return _notificationService.BroadcastAsync("wallet_rescan_started", new { walletId }, cancellationToken);
    }

    public Task NotifyWalletRescanCompletedAsync(Guid walletId, int utxoCount, long balanceSats, CancellationToken cancellationToken = default)
    {
        return _notificationService.BroadcastAsync("wallet_rescan_completed", new { walletId, utxoCount, balanceSats }, cancellationToken);
    }

    public Task NotifyWalletRescanFailedAsync(Guid walletId, string error, CancellationToken cancellationToken = default)
    {
        return _notificationService.BroadcastAsync("wallet_rescan_failed", new { walletId, error }, cancellationToken);
    }
}
