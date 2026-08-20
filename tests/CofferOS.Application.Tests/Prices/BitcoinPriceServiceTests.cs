using CofferOS.Application.Abstractions.Events;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Abstractions.Settings;
using CofferOS.Application.Contracts;
using CofferOS.Application.Prices;
using CofferOS.Domain.Common;
using CofferOS.Domain.Events;
using CofferOS.Domain.Prices;
using CofferOS.Infrastructure.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CofferOS.Application.Tests.Prices;

public class BitcoinPriceServiceTests
{
    private sealed class FakeProvider : IBitcoinPriceProvider
    {
        public string ProviderId { get; }
        public string DisplayName { get; }
        public DateTimeOffset? LastUpdated { get; }
        public decimal? NextPrice { get; set; }

        public FakeProvider(string id, string name, decimal? price)
        {
            ProviderId = id;
            DisplayName = name;
            NextPrice = price;
            LastUpdated = DateTimeOffset.UtcNow;
        }

        public Task<decimal?> GetCurrentPriceAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(NextPrice);
    }

    private sealed class FakeHistoryRepo : IBitcoinPriceHistoryRepository
    {
        public List<BitcoinPriceHistory> Saved { get; } = new();

        public Task<BitcoinPriceHistory?> GetLatestAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<BitcoinPriceHistory?>(Saved.Count > 0 ? Saved[^1] : null);

        public Task<IReadOnlyList<BitcoinPriceHistory>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BitcoinPriceHistory>>(Saved.TakeLast(count).ToList());

        public Task AddAsync(BitcoinPriceHistory entry, CancellationToken cancellationToken = default)
        {
            Saved.Add(entry);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDispatcher : IDomainEventDispatcher
    {
        public List<IDomainEvent> Dispatched { get; } = new();

        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            Dispatched.AddRange(domainEvents);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserSettingsService : IUserSettingsService
    {
        public UserSettingsDto Settings { get; set; } = new("USD", true, true, null);

        public Task<UserSettingsDto> GetAsync(CancellationToken ct = default) => Task.FromResult(Settings);

        public Task<UserSettingsDto> UpdateAsync(UpdateUserSettingsRequest request, CancellationToken ct = default)
        {
            Settings = new UserSettingsDto(request.Currency, request.EnableLivePriceUpdates, request.EnablePriceHistory, request.MempoolExplorerUrl);
            return Task.FromResult(Settings);
        }
    }

    private static BitcoinPriceOptions MakeOptions(bool enabled = true, bool privacy = false)
        => new BitcoinPriceOptions
        {
            Enabled = enabled,
            PrivacyMode = privacy,
            PollIntervalSeconds = 300,
            Provider = "manual"
        };

    private sealed class FakeExchangeRateProvider : IExchangeRateProvider
    {
        public IReadOnlyDictionary<string, decimal> GetCachedRates() =>
            new Dictionary<string, decimal> { ["usd"] = 100_000m };
    }

    private static BitcoinPriceService CreateService(
        IBitcoinPriceProvider currentProvider,
        IBitcoinPriceHistoryRepository? history = null,
        IDomainEventDispatcher? dispatcher = null,
        IMutableBitcoinPriceSource? mutable = null,
        BitcoinPriceOptions? options = null,
        IUserSettingsService? userSettings = null)
    {
        var opts = options ?? MakeOptions();
        return new BitcoinPriceService(
            currentProvider,
            mutable,
            history ?? new FakeHistoryRepo(),
            dispatcher ?? new FakeDispatcher(),
            new TestOptionsMonitor<BitcoinPriceOptions>(opts),
            userSettings ?? new FakeUserSettingsService(),
            new FakeExchangeRateProvider(),
            NullLogger<BitcoinPriceService>.Instance);
    }

    [Fact]
    public async Task RefreshAsync_WhenDisabled_DoesNotCallProvider()
    {
        var manual = new ManualBitcoinPriceProvider();
        var history = new FakeHistoryRepo();
        var dispatcher = new FakeDispatcher();

        var service = CreateService(manual, history, dispatcher, manual, MakeOptions(enabled: false));

        var result = await service.RefreshAsync();

        Assert.False(result.Success);
        Assert.Contains("disabled", result.Message ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Empty(history.Saved);
        Assert.Empty(dispatcher.Dispatched);
    }

    [Fact]
    public async Task RefreshAsync_WhenPrivacyMode_DoesNotCallProvider()
    {
        var manual = new ManualBitcoinPriceProvider();
        var history = new FakeHistoryRepo();
        var dispatcher = new FakeDispatcher();
        var fake = new FakeProvider("coingecko", "CoinGecko", 123456m);

        var service = CreateService(fake, history, dispatcher, manual, MakeOptions(privacy: true));

        var result = await service.RefreshAsync();

        Assert.False(result.Success);
        Assert.Contains("Privacy", result.Message ?? "");
        Assert.Empty(history.Saved);
        Assert.Empty(dispatcher.Dispatched);
    }

    [Fact]
    public async Task RefreshAsync_Success_PersistsHistory_UpdatesHolder_PublishesEvent()
    {
        var manual = new ManualBitcoinPriceProvider();
        var history = new FakeHistoryRepo();
        var dispatcher = new FakeDispatcher();
        var fake = new FakeProvider("coingecko", "CoinGecko", 98765.43m);

        var service = CreateService(fake, history, dispatcher, manual);

        var result = await service.RefreshAsync();

        Assert.True(result.Success);
        Assert.Equal(98765.43m, result.Price);
        Assert.Equal("coingecko", result.Provider);

        // History saved
        Assert.Single(history.Saved);
        Assert.Equal(98765.43m, history.Saved[0].PriceUsd);
        Assert.Equal("coingecko", history.Saved[0].Provider);

        // Holder updated
        var current = await manual.GetCurrentPriceAsync();
        Assert.Equal(98765.43m, current);

        // Event published
        var evt = Assert.Single(dispatcher.Dispatched) as PriceUpdatedEvent;
        Assert.NotNull(evt);
        Assert.Equal(98765.43m, evt.PriceUsd);
        Assert.Equal("coingecko", evt.Provider);
    }

    [Fact]
    public async Task GetCurrentPriceAsync_ReturnsValueFromHolder()
    {
        var manual = new ManualBitcoinPriceProvider();
        manual.SetPrice(111222.33m);

        var service = CreateService(manual);

        var price = await service.GetCurrentPriceAsync();

        Assert.Equal(111222.33m, price);
    }
}

/// <summary>Simple test helper to provide IOptionsMonitor behavior.</summary>
internal sealed class TestOptionsMonitor<T> : IOptionsMonitor<T> where T : class, new()
{
    private T _current;

    public TestOptionsMonitor(T current) => _current = current;

    public T CurrentValue => _current;

    public T Get(string? name) => _current;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
