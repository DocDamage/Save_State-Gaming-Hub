using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Netplay.Services;
using SaveState.Infrastructure.Netplay.Services;

namespace SaveState.Infrastructure.Netplay;

public static class DependencyInjection
{
    public static IServiceCollection AddNetplayServices(this IServiceCollection services)
    {
        // Core netplay services
        services.AddSingleton<IRetroNetplayService, RetroNetplayService>();
        services.AddSingleton<IMatchmakingQueue, InMemoryMatchmakingQueue>();
        services.AddSingleton<IRollbackNetcodeWrapper, RollbackNetcodeWrapper>();
        services.AddSingleton<ISpectatorRelayService, SpectatorRelayService>();

        return services;
    }
}
