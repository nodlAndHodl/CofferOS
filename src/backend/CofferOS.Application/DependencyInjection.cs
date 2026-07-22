using CofferOS.Application.Abstractions.Events;
using CofferOS.Application.Dashboard;
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
        services.AddScoped<DashboardService>();

        // Domain event handlers (each is resolved by the dispatcher as IDomainEventHandler<T>)
        services.AddScoped<IDomainEventHandler<WalletImportedEvent>, WalletImportedLoggingHandler>();
        services.AddScoped<IDomainEventHandler<WalletImportedEvent>, RescanOnWalletImportedHandler>();
        services.AddScoped<IDomainEventHandler<NewBlockDetectedEvent>, NewBlockLoggingHandler>();
        services.AddScoped<IDomainEventHandler<NewBlockDetectedEvent>, RescanOnNewBlockHandler>();

        return services;
    }
}
