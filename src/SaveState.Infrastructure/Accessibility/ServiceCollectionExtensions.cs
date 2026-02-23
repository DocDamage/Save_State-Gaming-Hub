using Microsoft.Extensions.DependencyInjection;
using SaveState.Infrastructure.Accessibility.Services;

namespace SaveState.Infrastructure.Accessibility;

/// <summary>
/// Extension methods for registering infrastructure accessibility services.
/// </summary>
public static class InfrastructureAccessibilityExtensions
{
    /// <summary>
    /// Adds infrastructure accessibility services to the service collection.
    /// </summary>
    public static IServiceCollection AddInfrastructureAccessibility(this IServiceCollection services)
    {
        // Screen reader service (platform-specific implementation)
        services.AddSingleton<IScreenReaderService, ScreenReaderService>();
        
        return services;
    }
}
