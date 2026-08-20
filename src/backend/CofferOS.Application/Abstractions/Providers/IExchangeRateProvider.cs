namespace CofferOS.Application.Abstractions.Providers;

/// <summary>
/// Provides BTC exchange rates against multiple fiat currencies.
/// Keys are lowercase ISO-4217 codes: "usd", "eur", "gbp", etc.
/// Values are the BTC price in that currency.
/// </summary>
public interface IExchangeRateProvider
{
    IReadOnlyDictionary<string, decimal> GetCachedRates();
}
