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
            // Only update if price has changed to avoid unnecessary updates
            if (Math.Abs(loan.CurrentBtcPrice - domainEvent.PriceUsd) > 0.01m)
            {
                loan.UpdatePrice(domainEvent.PriceUsd);
                updateCount++;
            }
        }

        if (updateCount > 0)
        {
            await _uow.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Updated BTC price to {Price} USD for {Count} active loans via {Provider}",
                domainEvent.PriceUsd, updateCount, domainEvent.Provider);
        }
    }
}
