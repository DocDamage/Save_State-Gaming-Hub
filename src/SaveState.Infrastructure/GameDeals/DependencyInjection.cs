// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.GameDeals;
using SaveState.Application.GameDeals;
using SaveState.Infrastructure.GameDeals.Clients;

namespace SaveState.Infrastructure.GameDeals;

/// <summary>
/// Extension methods for registering game deals services.
/// </summary>
public static class GameDealsServiceExtensions
{
    /// <summary>
    /// Adds game deals services to the DI container.
    /// </summary>
    public static IServiceCollection AddGameDealsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<IsThereAnyDealOptions>(
            configuration.GetSection("GameDeals:IsThereAnyDeal"));

        // Register service
        services.AddScoped<IGameDealsService, GameDealsService>();

        // Register repository
        services.AddScoped<IGameDealsRepository, GameDealsRepository>();

        // Register API clients
        services.AddHttpClient<IsThereAnyDealClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.isthereanydeal.com/v01/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register deal source clients
        services.AddScoped<IDealSourceClient, IsThereAnyDealClient>();

        // Register background services
        services.AddHostedService<PriceAlertBackgroundService>();
        services.AddHostedService<DealRefreshBackgroundService>();

        return services;
    }
}
