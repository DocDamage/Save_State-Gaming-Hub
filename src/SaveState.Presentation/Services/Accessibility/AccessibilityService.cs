using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace SaveState.Presentation.Services.Accessibility;

/// <summary>
/// Implementation of the accessibility service for WCAG 2.1 AA compliance.
/// </summary>
public class AccessibilityService : IAccessibilityService
{
    private readonly ILogger<AccessibilityService> _logger;
    private readonly Core.Common.Services.IAccessibilityService _coreAccessibilityService;
    private Control? _focusTrapContainer;
    private Control? _previouslyFocusedElement;
    private double _textScaleFactor = 1.0;
    private bool _isKeyboardNavigationEnabled = true;
    private ColorFilterType? _activeColorFilter;

    public bool IsHighContrastEnabled => _coreAccessibilityService.IsHighContrastEnabled;
    public bool IsReducedMotionEnabled { get; private set; }
    public double TextScaleFactor => _textScaleFactor;
    public bool IsKeyboardNavigationEnabled => _isKeyboardNavigationEnabled;

    public event EventHandler<bool>? HighContrastChanged;
    public event EventHandler<bool>? ReducedMotionChanged;

    public AccessibilityService(
        ILogger<AccessibilityService> logger,
        Core.Common.Services.IAccessibilityService coreAccessibilityService)
    {
        _logger = logger;
        _coreAccessibilityService = coreAccessibilityService;
        
        // Subscribe to core service events
        // Note: The core service doesn't have events in current implementation
        // but we'll set up our own state tracking
    }

    #region Screen Reader Support

    public async Task AnnounceAsync(string message, AccessibilityPriority priority = AccessibilityPriority.Normal)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("Attempted to announce empty message");
            return;
        }

        try
        {
            // Map to core service priority
            var corePriority = priority switch
            {
                AccessibilityPriority.Low => AnnouncementPriority.Low,
                AccessibilityPriority.Normal => AnnouncementPriority.Normal,
                AccessibilityPriority.High => AnnouncementPriority.High,
                AccessibilityPriority.Critical => AnnouncementPriority.Critical,
                _ => AnnouncementPriority.Normal
            };

            await _coreAccessibilityService.AnnounceAsync(message, corePriority);
            
            // Additional UI Automation announcement for Windows
            AnnounceToUiAutomation(message, priority);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to announce message: {Message}", message);
        }
    }

    public async Task AnnounceAlertAsync(string alert)
    {
        await AnnounceAsync(alert, AccessibilityPriority.High);
    }

    public Task SetAriaLabelAsync(Control element, string label)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        
        AutomationProperties.SetName(element, label);
        _logger.LogDebug("Set ARIA label for {Type}: {Label}", element.GetType().Name, label);
        
        return Task.CompletedTask;
    }

    public Task SetAriaDescriptionAsync(Control element, string description)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        
        AutomationProperties.SetHelpText(element, description);
        _logger.LogDebug("Set ARIA description for {Type}: {Description}", element.GetType().Name, description);
        
        return Task.CompletedTask;
    }

    public Task SetAriaLiveAsync(Control element, AriaLiveMode mode)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        
        // Avalonia doesn't have built-in ARIA live support, so we store it in attached properties
        element.SetValue(AriaLiveProperty, mode);
        
        _logger.LogDebug("Set ARIA live mode for {Type}: {Mode}", element.GetType().Name, mode);
        
        return Task.CompletedTask;
    }

    // Attached property for ARIA live regions
    public static readonly AttachedProperty<AriaLiveMode> AriaLiveProperty =
        AvaloniaProperty.RegisterAttached<AccessibilityService, Control, AriaLiveMode>(
            "AriaLive", AriaLiveMode.Off);

    #endregion

    #region Focus Management

    public Task MoveFocusAsync(FocusNavigationDirection direction)
    {
        try
        {
            var window = GetActiveWindow();
            if (window == null) return Task.CompletedTask;

            var focused = window.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.IsFocused);
            if (focused == null) return Task.CompletedTask;

            // Try to move focus
            var nextElement = FindNextFocusableElement(focused, direction);
            if (nextElement != null && nextElement.Focusable)
            {
                nextElement.Focus();
                _logger.LogDebug("Moved focus to {ElementType}", nextElement.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move focus");
        }

        return Task.CompletedTask;
    }

    public Task SetFocusAsync(Control element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));

        try
        {
            if (element.Focusable)
            {
                element.Focus();
                _logger.LogDebug("Set focus to {ElementType}", element.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set focus to element");
        }

        return Task.CompletedTask;
    }

    public Task TrapFocusAsync(Control container)
    {
        if (container == null) throw new ArgumentNullException(nameof(container));

        _focusTrapContainer = container;
        var activeWindow = GetActiveWindow();
        _previouslyFocusedElement = activeWindow?.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.IsFocused);

        // Subscribe to focus changes
        container.AddHandler(InputElement.LostFocusEvent, OnFocusLeavingContainer, RoutingStrategies.Bubble);

        // Focus first element in container
        FocusFirstAsync(container);

        _logger.LogInformation("Focus trapped in {ContainerType}", container.GetType().Name);
        return Task.CompletedTask;
    }

    public Task ReleaseFocusTrapAsync()
    {
        if (_focusTrapContainer != null)
        {
            _focusTrapContainer.RemoveHandler(InputElement.LostFocusEvent, OnFocusLeavingContainer);
            _focusTrapContainer = null;

            // Restore previous focus
            if (_previouslyFocusedElement != null)
            {
                _previouslyFocusedElement.Focus();
                _previouslyFocusedElement = null;
            }

            _logger.LogInformation("Focus trap released");
        }

        return Task.CompletedTask;
    }

    public Task FocusFirstAsync(Control container)
    {
        if (container == null) throw new ArgumentNullException(nameof(container));

        var firstFocusable = GetFocusableElements(container).FirstOrDefault();
        if (firstFocusable != null)
        {
            firstFocusable.Focus();
            _logger.LogDebug("Focused first element in container");
        }

        return Task.CompletedTask;
    }

    public Task FocusLastAsync(Control container)
    {
        if (container == null) throw new ArgumentNullException(nameof(container));

        var lastFocusable = GetFocusableElements(container).LastOrDefault();
        if (lastFocusable != null)
        {
            lastFocusable.Focus();
            _logger.LogDebug("Focused last element in container");
        }

        return Task.CompletedTask;
    }

    private void OnFocusLeavingContainer(object? sender, RoutedEventArgs e)
    {
        if (_focusTrapContainer == null) return;

        // Check if focus is leaving the container
        var activeWindow = GetActiveWindow();
        var focusedElement = activeWindow?.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.IsFocused);
        if (focusedElement != null && !IsDescendantOf(focusedElement, _focusTrapContainer))
        {
            // Trap focus by moving back to first or last element
            var focusableElements = GetFocusableElements(_focusTrapContainer).ToList();
            if (focusableElements.Count > 0)
            {
                focusableElements[0].Focus();
                e.Handled = true;
            }
        }
    }

    private bool IsDescendantOf(Control element, Control ancestor)
    {
        var current = element.Parent;
        while (current != null)
        {
            if (current == ancestor) return true;
            current = current.Parent;
        }
        return false;
    }

    private IEnumerable<Control> GetFocusableElements(Control container)
    {
        return container.GetVisualDescendants()
            .OfType<Control>()
            .Where(c => c.Focusable && c.IsVisible && c.IsEnabled)
            .OrderBy(GetTabIndex);
    }

    private int GetTabIndex(Control control)
    {
        return control.TabIndex;
    }

    private Control? FindNextFocusableElement(Control current, global::SaveState.Presentation.Services.Accessibility.FocusNavigationDirection direction)
    {
        var window = GetActiveWindow();
        if (window == null) return null;

        var focusableElements = window.GetVisualDescendants()
            .OfType<Control>()
            .Where(c => c.Focusable && c.IsVisible && c.IsEnabled)
            .ToList();

        var currentIndex = focusableElements.IndexOf(current);
        if (currentIndex < 0) return null;

        return direction switch
        {
            global::SaveState.Presentation.Services.Accessibility.FocusNavigationDirection.Next => focusableElements.ElementAtOrDefault(currentIndex + 1),
            global::SaveState.Presentation.Services.Accessibility.FocusNavigationDirection.Previous => focusableElements.ElementAtOrDefault(currentIndex - 1),
            global::SaveState.Presentation.Services.Accessibility.FocusNavigationDirection.First => focusableElements.FirstOrDefault(),
            global::SaveState.Presentation.Services.Accessibility.FocusNavigationDirection.Last => focusableElements.LastOrDefault(),
            _ => null
        };
    }

    private Window? GetActiveWindow()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    #endregion

    #region High Contrast

    public async Task EnableHighContrastAsync()
    {
        try
        {
            await _coreAccessibilityService.EnableHighContrastAsync();
            HighContrastChanged?.Invoke(this, true);
            await AnnounceAsync("High contrast mode enabled", AccessibilityPriority.High);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable high contrast");
        }
    }

    public async Task DisableHighContrastAsync()
    {
        try
        {
            await _coreAccessibilityService.DisableHighContrastAsync();
            HighContrastChanged?.Invoke(this, false);
            await AnnounceAsync("High contrast mode disabled", AccessibilityPriority.Normal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable high contrast");
        }
    }

    #endregion

    #region Reduced Motion

    public async Task EnableReducedMotionAsync()
    {
        IsReducedMotionEnabled = true;
        ReducedMotionChanged?.Invoke(this, true);
        await AnnounceAsync("Reduced motion enabled", AccessibilityPriority.High);
        _logger.LogInformation("Reduced motion enabled");
    }

    public async Task DisableReducedMotionAsync()
    {
        IsReducedMotionEnabled = false;
        ReducedMotionChanged?.Invoke(this, false);
        await AnnounceAsync("Reduced motion disabled", AccessibilityPriority.Normal);
        _logger.LogInformation("Reduced motion disabled");
    }

    #endregion

    #region Color Filters

    public Task EnableColorFilterAsync(ColorFilterType filter)
    {
        _activeColorFilter = filter;
        
        // Apply color filter to the application
        var window = GetActiveWindow();
        if (window != null)
        {
            var effect = CreateColorFilterEffect(filter);
            // Note: Avalonia doesn't have built-in color filter effects
            // This would require custom shader implementation
        }

        _logger.LogInformation("Color filter enabled: {Filter}", filter);
        return Task.CompletedTask;
    }

    public Task DisableColorFilterAsync()
    {
        _activeColorFilter = null;
        _logger.LogInformation("Color filter disabled");
        return Task.CompletedTask;
    }

    private IEffect? CreateColorFilterEffect(ColorFilterType filter)
    {
        // This would create platform-specific color filter effects
        // For now, return null as Avalonia doesn't have built-in support
        return null;
    }

    #endregion

    #region Text Scaling

    public async Task SetTextScaleAsync(double scale)
    {
        if (scale < 0.5 || scale > 3.0)
        {
            _logger.LogWarning("Invalid text scale: {Scale}. Must be between 0.5 and 3.0", scale);
            return;
        }

        _textScaleFactor = scale;
        await _coreAccessibilityService.SetFontSizeMultiplierAsync((float)scale);
        
        await AnnounceAsync($"Text size changed to {scale:P0}", AccessibilityPriority.Normal);
        _logger.LogInformation("Text scale set to {Scale}", scale);
    }

    #endregion

    #region Keyboard Navigation

    public Task EnableKeyboardNavigationAsync()
    {
        _isKeyboardNavigationEnabled = true;
        _logger.LogInformation("Keyboard navigation enabled");
        return Task.CompletedTask;
    }

    public Task DisableKeyboardNavigationAsync()
    {
        _isKeyboardNavigationEnabled = false;
        _logger.LogInformation("Keyboard navigation disabled");
        return Task.CompletedTask;
    }

    #endregion

    #region Accessibility Tree

    public AccessibilityNode? GetAccessibilityTree()
    {
        var window = GetActiveWindow();
        if (window == null) return null;

        return BuildAccessibilityTree(window);
    }

    public async Task DumpAccessibilityTreeAsync(string outputPath)
    {
        try
        {
            var tree = GetAccessibilityTree();
            if (tree == null)
            {
                _logger.LogWarning("Could not generate accessibility tree");
                return;
            }

            var json = JsonSerializer.Serialize(tree, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(outputPath, json);
            _logger.LogInformation("Accessibility tree dumped to {Path}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dump accessibility tree");
        }
    }

    private AccessibilityNode BuildAccessibilityTree(Control element)
    {
        var node = new AccessibilityNode
        {
            Role = element.GetType().Name,
            Name = AutomationProperties.GetName(element) ?? GetAccessibleName(element),
            Description = AutomationProperties.GetHelpText(element),
            IsEnabled = element.IsEnabled,
            IsFocused = element.IsFocused
        };

        if (element is ContentControl contentControl && contentControl.Content is string content)
        {
            node.Value = content;
        }

        foreach (var child in element.GetVisualChildren().OfType<Control>())
        {
            node.Children.Add(BuildAccessibilityTree(child));
        }

        return node;
    }

    private string GetAccessibleName(Control element)
    {
        // Try to derive accessible name from various properties
        if (element is HeaderedContentControl headered)
        {
            return headered.Header?.ToString() ?? string.Empty;
        }

        if (element is ContentControl content && content.Content is string text)
        {
            return text;
        }

        if (element is TextBlock textBlock)
        {
            return textBlock.Text ?? string.Empty;
        }

        return element.Name ?? element.GetType().Name;
    }

    #endregion

    #region Platform Integration

    private void AnnounceToUiAutomation(string message, AccessibilityPriority priority)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, we would use UI Automation RaiseNotificationEvent
            // This requires platform-specific interop
            _logger.LogDebug("UI Automation announcement: {Message}", message);
        }
    }

    #endregion
}
