namespace CofferOS.Application.Contracts;

public sealed record UserSettingsDto(
    string Currency,
    bool EnableLivePriceUpdates,
    bool EnablePriceHistory,
    string? MempoolExplorerUrl
);

public sealed record UpdateUserSettingsRequest(
    string Currency,
    bool EnableLivePriceUpdates,
    bool EnablePriceHistory,
    string? MempoolExplorerUrl
);
