using CofferOS.Application.CostBasis;
using CofferOS.Application.Abstractions.Dashboard;
using CofferOS.Application.Abstractions.Events;
using CofferOS.Application.Abstractions.Holdings;
using CofferOS.Application.Abstractions.Treasury;
using CofferOS.Application.Dashboard;
using CofferOS.Application.Holdings;
using CofferOS.Application.Prices;
using CofferOS.Application.Retirement;
using CofferOS.Application.Treasury;
using CofferOS.Application.Wallets;
using CofferOS.Application.Wallets.EventHandlers;
using CofferOS.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace CofferOS.Application;

/// <summary>Registers application-layer services and domain event handlers.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Use-case / query services
        services.AddScoped<WalletImportService>();
        services.AddScoped<WalletQueryService>();
        services.AddScoped<WalletRescanService>();
        services.AddScoped<TransactionMetadataService>();
        services.AddScoped<CostBasisService>();
        services.AddScoped<WalletTimelineService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<TreasuryService>();
        services.AddScoped<RetirementAccountService>();
        services.AddScoped<ILoanAccrualService, LoanAccrualService>();

        // Dashboard / Holdings / Treasury aggregation services
        services.AddScoped<IHoldingsService, HoldingsService>();
        services.AddScoped<ILoanAnalyticsService, LoanAnalyticsService>();
        services.AddScoped<IDashboardQueryService, DashboardQueryService>();

        // Domain event handlers (each is resolved by the dispatcher as IDomainEventHandler<T>)
        services.AddScoped<IDomainEventHandler<WalletImportedEvent>, WalletImportedLoggingHandler>();
        services.AddScoped<IDomainEventHandler<WalletImportedEvent>, RescanOnWalletImportedHandler>();
        services.AddScoped<IDomainEventHandler<NewBlockDetectedEvent>, NewBlockLoggingHandler>();
        services.AddScoped<IDomainEventHandler<NewBlockDetectedEvent>, RescanOnNewBlockHandler>();

        // Bitcoin price engine event handlers
        services.AddScoped<IDomainEventHandler<PriceUpdatedEvent>, PriceUpdatedLoggingHandler>();
        services.AddScoped<IDomainEventHandler<PriceUpdatedEvent>, PriceUpdatedLoanHandler>();

        return services;
    }
}
