// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Subscriptions;
using SaveState.Core.Subscriptions.Authentication;
using SaveState.Application.Subscriptions;
using SaveState.Infrastructure.Subscriptions.Providers;
using SaveState.Infrastructure.Subscriptions.Authentication;
using SaveState.Infrastructure.Subscriptions.Clients;

namespace SaveState.Infrastructure.Subscriptions;

/// <summary>
/// Extension methods for registering subscription services in the DI container.
/// </summary>
public static class SubscriptionServiceExtensions
{
    /// <summary>
    /// Adds subscription services and providers to the DI container.
    /// </summary>
    public static IServiceCollection AddSubscriptionServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register main service
        services.AddScoped<ISubscriptionService, SubscriptionManagerService>();
        
        // Register repository
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        
        // Configure options
        services.Configure<XboxGamePassOptions>(
            configuration.GetSection("Subscriptions:XboxGamePass"));
        services.Configure<PlayStationPlusOptions>(
            configuration.GetSection("Subscriptions:PlayStationPlus"));
        services.Configure<EaPlayOptions>(
            configuration.GetSection("Subscriptions:EaPlay"));
        services.Configure<UbisoftPlusOptions>(
            configuration.GetSection("Subscriptions:UbisoftPlus"));
        services.Configure<GeForceNowOptions>(
            configuration.GetSection("Subscriptions:GeForceNow"));
        
        // Register OAuth token store
        services.AddSingleton<IOAuthTokenStore, SecureOAuthTokenStore>();

        // Register API clients
        services.AddHttpClient<XboxCatalogClient>(client =>
        {
            client.BaseAddress = new Uri("https://displaycatalog.mp.microsoft.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register HTTP clients for each provider
        services.AddHttpClient<XboxGamePassProvider>(client =>
        {
            client.BaseAddress = new Uri("https://displaycatalog.mp.microsoft.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        services.AddHttpClient<PlayStationPlusProvider>(client =>
        {
            client.BaseAddress = new Uri("https://web.np.playstation.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        services.AddHttpClient<EaPlayProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.ea.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        services.AddHttpClient<UbisoftPlusProvider>(client =>
        {
            client.BaseAddress = new Uri("https://public-ubiservices.ubi.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        services.AddHttpClient<NvidiaGeForceNowProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.nvidia.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        // Register all providers
        services.AddScoped<ISubscriptionProvider, XboxGamePassProvider>();
        services.AddScoped<ISubscriptionProvider, PlayStationPlusProvider>();
        services.AddScoped<ISubscriptionProvider, EaPlayProvider>();
        services.AddScoped<ISubscriptionProvider, UbisoftPlusProvider>();
        services.AddScoped<ISubscriptionProvider, NvidiaGeForceNowProvider>();

        // Register background services
        services.AddHostedService<SubscriptionSyncBackgroundService>();
        services.AddHostedService<SubscriptionAlertService>();
        
        return services;
    }
}
