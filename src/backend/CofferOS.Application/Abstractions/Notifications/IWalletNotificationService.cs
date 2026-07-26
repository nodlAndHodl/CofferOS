namespace CofferOS.Application.Abstractions.Notifications;

/// <summary>Service for notifying clients about wallet-related events.</summary>
public interface IWalletNotificationService
{
    Task NotifyWalletImportedAsync(Guid walletId, string walletName, CancellationToken cancellationToken = default);
    Task NotifyWalletRescanStartedAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task NotifyWalletRescanCompletedAsync(Guid walletId, int utxoCount, long balanceSats, CancellationToken cancellationToken = default);
    Task NotifyWalletRescanFailedAsync(Guid walletId, string error, CancellationToken cancellationToken = default);
}
