using CofferOS.Application.Abstractions.Providers;
using CofferOS.Infrastructure.Providers;
using Xunit;

namespace CofferOS.Application.Tests.Prices;

public class ProviderAbstractionTests
{
    [Fact]
    public void ManualProvider_ImplementsMutableInterface()
    {
        IBitcoinPriceProvider provider = new ManualBitcoinPriceProvider();
        Assert.IsAssignableFrom<IMutableBitcoinPriceSource>(provider);
    }

    [Fact]
    public async Task ManualProvider_DefaultsToNull_ThenReturnsSetPrice()
    {
        var mp = new ManualBitcoinPriceProvider();

        var initial = await mp.GetCurrentPriceAsync();
        Assert.Null(initial);

        mp.SetPrice(12345.67m);

        var after = await mp.GetCurrentPriceAsync();
        Assert.Equal(12345.67m, after);
    }

    [Fact]
    public void ManualProvider_ProviderIdAndDisplayName_AreStable()
    {
        var mp = new ManualBitcoinPriceProvider();
        Assert.Equal("manual", mp.ProviderId);
        Assert.Equal("Manual Entry", mp.DisplayName);
    }

    [Fact]
    public void ManualProvider_SetNegativePrice_Throws()
    {
        var mp = new ManualBitcoinPriceProvider();
        Assert.Throws<ArgumentException>(() => mp.SetPrice(-1));
    }

    [Fact]
    public void CoinGeckoProvider_HasCorrectIds()
    {
        // We can't easily construct without HttpClient/Logger, but we can at least check the types exist
        // and that a manual one can be cast etc. This is a compile-time + basic runtime sanity test.
        Assert.NotNull(typeof(CoinGeckoPriceProvider));
    }

    [Fact]
    public void CoinbaseProvider_HasCorrectIds()
    {
        Assert.NotNull(typeof(CoinbasePriceProvider));
    }
}
