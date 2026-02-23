using Microsoft.Extensions.DependencyInjection;
using SaveState.Infrastructure.Accessibility.Services;
using SaveState.Presentation.Services.Keyboard;

namespace SaveState.Presentation.Services.Accessibility;

/// <summary>
/// Extension methods for registering accessibility services.
/// </summary>
public static class AccessibilityServiceCollectionExtensions
{
    /// <summary>
    /// Adds accessibility services to the service collection.
    /// </summary>
    public static IServiceCollection AddAccessibilityServices(this IServiceCollection services)
    {
        // Register presentation layer accessibility service
        services.AddSingleton<IAccessibilityService, AccessibilityService>();
        
        // Register keyboard navigation service
        services.AddSingleton<IKeyboardNavigationService, KeyboardNavigationService>();
        
        // Register screen reader service
        services.AddSingleton<IScreenReaderService, ScreenReaderService>();
        
        // Register accessibility auditor
        services.AddSingleton<AccessibilityAuditor>();
        
        // ColorContrastChecker is a static utility class, no DI registration needed
        
        return services;
    }
}
