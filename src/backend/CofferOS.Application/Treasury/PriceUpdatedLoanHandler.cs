using CofferOS.Application.Abstractions.Events;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CofferOS.Application.Treasury;

/// <summary>
/// Handles PriceUpdatedEvent by refreshing CurrentBtcPrice on all active loans.
/// This ensures collateral value and LTV calculations always use the latest market price.
/// </summary>
public sealed class PriceUpdatedLoanHandler : IDomainEventHandler<PriceUpdatedEvent>
{
    private readonly ILoanRepository _loans;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PriceUpdatedLoanHandler> _logger;

    public PriceUpdatedLoanHandler(
        ILoanRepository loans,
        IUnitOfWork uow,
        ILogger<PriceUpdatedLoanHandler> logger)
    {
        _loans = loans;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(PriceUpdatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var activeLoans = await _loans.GetActiveAsync(cancellationToken);
        if (activeLoans.Count == 0)
        {
            _logger.LogDebug("No active loans to update with new price {Price}", domainEvent.PriceUsd);
            return;
        }

        var updateCount = 0;
        foreach (var loan in activeLoans)
        {
            // Resolve BTC price in the loan's denomination currency.
            // ExchangeRates values are direct BTC prices per currency from CoinGecko (not conversion factors).
            var priceInLoanCurrency = ResolvePriceForLoan(domainEvent, loan.Currency);

            if (Math.Abs(loan.CurrentBtcPrice - priceInLoanCurrency) > 0.01m)
            {
                loan.UpdatePrice(priceInLoanCurrency);
                updateCount++;
            }
        }

        if (updateCount > 0)
        {
            await _uow.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Updated BTC price for {Count} active loans via {Provider} (base USD: {Price})",
                updateCount, domainEvent.Provider, domainEvent.PriceUsd);
        }
    }

    private static decimal ResolvePriceForLoan(PriceUpdatedEvent evt, string loanCurrency)
    {
        if (string.IsNullOrEmpty(loanCurrency) || loanCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase))
            return evt.PriceUsd;

        if (evt.ExchangeRates is not null &&
            evt.ExchangeRates.TryGetValue(loanCurrency.ToLowerInvariant(), out var price) &&
            price > 0)
            return price;

        // Exchange rates not available (e.g. manual price provider) — fall back to USD
        return evt.PriceUsd;
    }
}
