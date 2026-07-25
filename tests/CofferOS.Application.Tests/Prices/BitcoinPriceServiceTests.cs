using CofferOS.Application.Abstractions.Events;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Prices;
using CofferOS.Domain.Events;
using CofferOS.Domain.Prices;
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
        public decimal? NextPrice { get; set; }

        public FakeProvider(string id, string name, decimal? price)
        {
            ProviderId = id;
            DisplayName = name;
            NextPrice = price;
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

    private static BitcoinPriceOptions MakeOptions(string provider = "manual", bool enabled = true, bool privacy = false, int interval = 300)
        => new BitcoinPriceOptions
        {
            Enabled = enabled,
            PrivacyMode = privacy,
            PollIntervalSeconds = interval,
            Provider = provider
        };

    [Fact]
    public async Task RefreshAsync_WhenDisabled_DoesNotCallProvider()
    {
        var manual = new ManualBitcoinPriceProvider();
        var history = new FakeHistoryRepo();
        var dispatcher = new FakeDispatcher();
        var opts = Options.Create(MakeOptions(enabled: false));
        var optsMonitor = new TestOptionsMonitor<BitcoinPriceOptions>(opts.Value);

        var service = new BitcoinPriceService(
            manual,
            manual,
            history,
            dispatcher,
            optsMonitor,
            new[] { (IBitcoinPriceProvider)manual },
            NullLogger<BitcoinPriceService>.Instance);

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
        var opts = Options.Create(MakeOptions(privacy: true));
        var optsMonitor = new TestOptionsMonitor<BitcoinPriceOptions>(opts.Value);

        var fake = new FakeProvider("coingecko", "CoinGecko", 123456m);

        var service = new BitcoinPriceService(
            manual,
            manual,
            history,
            dispatcher,
            optsMonitor,
            new[] { (IBitcoinPriceProvider)manual, fake },
            NullLogger<BitcoinPriceService>.Instance);

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
        var opts = Options.Create(MakeOptions(provider: "coingecko"));
        var optsMonitor = new TestOptionsMonitor<BitcoinPriceOptions>(opts.Value);

        var fake = new FakeProvider("coingecko", "CoinGecko", 98765.43m);

        var service = new BitcoinPriceService(
            manual,
            manual,
            history,
            dispatcher,
            optsMonitor,
            new[] { (IBitcoinPriceProvider)manual, fake },
            NullLogger<BitcoinPriceService>.Instance);

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

        var history = new FakeHistoryRepo();
        var dispatcher = new FakeDispatcher();
        var opts = Options.Create(MakeOptions());
        var optsMonitor = new TestOptionsMonitor<BitcoinPriceOptions>(opts.Value);

        var service = new BitcoinPriceService(
            manual,
            manual,
            history,
            dispatcher,
            optsMonitor,
            new[] { (IBitcoinPriceProvider)manual },
            NullLogger<BitcoinPriceService>.Instance);

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
