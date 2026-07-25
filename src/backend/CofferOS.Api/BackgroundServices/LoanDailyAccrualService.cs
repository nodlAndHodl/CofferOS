using CofferOS.Application.Abstractions.Events;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Treasury;
using CofferOS.Domain.Common;
using CofferOS.Domain.Events;
using CofferOS.Domain.Treasury;
using Microsoft.Extensions.DependencyInjection;

namespace CofferOS.Api.BackgroundServices;

/// <summary>
/// Daily background worker that accrues simple daily interest for all active loans.
/// Runs once per day (at startup + every 24h).
/// </summary>
public sealed class LoanDailyAccrualService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LoanDailyAccrualService> _logger;

    public LoanDailyAccrualService(IServiceProvider services, ILogger<LoanDailyAccrualService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small delay to let the app fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAccrualCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loan accrual cycle failed");
            }

            try
            {
                // Run roughly once per day. A real implementation could schedule at a fixed time (e.g. 00:05 local).
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }
    }

    private async Task RunAccrualCycleAsync(CancellationToken ct)
    {
        await using var scope = _services.CreateAsyncScope();
        var accrual = scope.ServiceProvider.GetRequiredService<ILoanAccrualService>();
        var loansRepo = scope.ServiceProvider.GetRequiredService<ILoanRepository>();
        var paymentsRepo = scope.ServiceProvider.GetRequiredService<ILoanPaymentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        var activeLoans = await loansRepo.GetActiveAsync(ct);
        if (activeLoans.Count == 0)
        {
            _logger.LogDebug("No active loans to accrue.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var eventsToDispatch = new List<IDomainEvent>();

        foreach (var loan in activeLoans)
        {
            try
            {
                // Load historical payments for accurate principal reduction
                var payments = await paymentsRepo.GetByLoanAsync(loan.Id, ct);

                // Accrue using the engine (mutates loan)
                var interestAdded = await accrual.AccrueSimpleDailyInterestAsync(loan, now, ct);

                if (interestAdded > 0)
                {
                    _logger.LogInformation("Accrued {Interest} to loan {LoanId} ({Name})", interestAdded, loan.Id, loan.Name);
                }

                // Recompute snapshot for derived values (optional, for logging)
                var snapshot = await accrual.CalculateAsync(loan, payments, now, ct);

                // Raise domain event so other parts of the system can react
                eventsToDispatch.Add(new LoanUpdatedEvent(loan.Id, now));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to accrue loan {LoanId}", loan.Id);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);

        if (eventsToDispatch.Count > 0)
        {
            await dispatcher.DispatchAsync(eventsToDispatch, ct);
        }

        _logger.LogInformation("Loan accrual cycle complete for {Count} active loans", activeLoans.Count);
    }
}
