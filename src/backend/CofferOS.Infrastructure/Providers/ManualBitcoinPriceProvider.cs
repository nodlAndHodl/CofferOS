using CofferOS.Application.Abstractions.Providers;

namespace CofferOS.Infrastructure.Providers;

/// <summary>
/// A singleton-backed manual price provider. The UI (or any caller) can set a price
/// via IMutableBitcoinPriceSource, and GetCurrentPriceAsync returns that value.
/// This is the initial implementation; later providers can be added without changing consumers.
/// </summary>
public sealed class ManualBitcoinPriceProvider : IMutableBitcoinPriceSource
{
    private decimal? _price;

    public string ProviderId => "manual";
    public string DisplayName => "Manual Entry";

    public Task<decimal?> GetCurrentPriceAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_price);

    public void SetPrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(price));
        _price = price;
    }
}
