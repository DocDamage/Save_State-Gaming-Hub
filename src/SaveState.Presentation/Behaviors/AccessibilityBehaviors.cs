using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services.Accessibility;

namespace SaveState.Presentation.Behaviors;

/// <summary>
/// Base class for accessibility behaviors.
/// </summary>
public abstract class AccessibilityBehavior<T> : Behavior<T> where T : Control
{
    protected ILogger? Logger { get; set; }
    protected IAccessibilityService? AccessibilityService { get; set; }

    protected override void OnAttached()
    {
        base.OnAttached();
        
        // Try to get services from application service provider
        try
        {
            if (Avalonia.Application.Current is { } app)
            {
                // Services would typically be resolved from DI container
                // This is a simplified version
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error initializing accessibility behavior");
        }
    }

    protected async Task AnnounceAsync(string message, AccessibilityPriority priority = AccessibilityPriority.Normal)
    {
        if (AccessibilityService != null)
        {
            await AccessibilityService.AnnounceAsync(message, priority);
        }
    }
}

/// <summary>
/// Makes buttons accessible with proper announcements and keyboard handling.
/// </summary>
public class AccessibleButtonBehavior : AccessibilityBehavior<Button>
{
    private string? _originalAutomationName;

    protected override void OnAttached()
    {
        base.OnAttached();
        
        if (AssociatedObject == null) return;

        AssociatedObject.GotFocus += OnGotFocus;
        AssociatedObject.LostFocus += OnLostFocus;
        AssociatedObject.Click += OnClick;
        AssociatedObject.KeyDown += OnKeyDown;

        // Ensure button is focusable
        if (!AssociatedObject.Focusable)
        {
            AssociatedObject.Focusable = true;
        }

        // Set default automation properties if not set
        if (string.IsNullOrEmpty(AutomationProperties.GetName(AssociatedObject)))
        {
            var content = AssociatedObject.Content?.ToString();
            if (!string.IsNullOrEmpty(content))
            {
                AutomationProperties.SetName(AssociatedObject, content);
            }
        }

        // Set help text if not set
        if (string.IsNullOrEmpty(AutomationProperties.GetHelpText(AssociatedObject)))
        {
            AutomationProperties.SetHelpText(AssociatedObject, "Press Enter or Space to activate");
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        
        if (AssociatedObject == null) return;

        AssociatedObject.GotFocus -= OnGotFocus;
        AssociatedObject.LostFocus -= OnLostFocus;
        AssociatedObject.Click -= OnClick;
        AssociatedObject.KeyDown -= OnKeyDown;
    }

    private void OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (AssociatedObject == null) return;

        var buttonName = AutomationProperties.GetName(AssociatedObject) ?? AssociatedObject.Content?.ToString() ?? "Button";
        _ = AnnounceAsync($"{buttonName} button focused", AccessibilityPriority.Low);
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        // Could announce lost focus if needed
    }

    private void OnClick(object? sender, RoutedEventArgs e)
    {
        if (AssociatedObject == null) return;

        var buttonName = AutomationProperties.GetName(AssociatedObject) ?? AssociatedObject.Content?.ToString() ?? "Button";
        _ = AnnounceAsync($"{buttonName} activated", AccessibilityPriority.Normal);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Handle Enter and Space for button activation
        if (e.Key == Key.Enter || e.Key == Key.Space)
        {
            if (AssociatedObject is Button button)
            {
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            e.Handled = true;
        }
    }
}

/// <summary>
/// Makes text input controls accessible with proper announcements.
/// </summary>
public class AccessibleInputBehavior : AccessibilityBehavior<TextBox>
{
    private string? _previousValue;
    private int _previousSelectionStart;

    protected override void OnAttached()
    {
        base.OnAttached();
        
        if (AssociatedObject == null) return;

        AssociatedObject.GotFocus += OnGotFocus;
        AssociatedObject.TextChanged += OnTextChanged;
        AssociatedObject.KeyDown += OnKeyDown;

        // Set default automation properties
        if (string.IsNullOrEmpty(AutomationProperties.GetName(AssociatedObject)))
        {
            var watermark = AssociatedObject.Watermark;
            if (!string.IsNullOrEmpty(watermark))
            {
                AutomationProperties.SetName(AssociatedObject, watermark);
            }
        }

        // Store initial value
        _previousValue = AssociatedObject.Text;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        
        if (AssociatedObject == null) return;

        AssociatedObject.GotFocus -= OnGotFocus;
        AssociatedObject.TextChanged -= OnTextChanged;
        AssociatedObject.KeyDown -= OnKeyDown;
    }

    private void OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (AssociatedObject == null) return;

        var fieldName = AutomationProperties.GetName(AssociatedObject) ?? AssociatedObject.Watermark ?? "Text field";
        var inputType = AssociatedObject.PasswordChar != '\0' ? "password" : "text";
        var hasValue = !string.IsNullOrEmpty(AssociatedObject.Text);
        
        var announcement = hasValue 
            ? $"{fieldName} {inputType} field, value entered"
            : $"{fieldName} {inputType} field";
            
        _ = AnnounceAsync(announcement, AccessibilityPriority.Low);
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (AssociatedObject == null) return;

        var currentValue = AssociatedObject.Text ?? string.Empty;
        
        // Don't announce password changes
        if (AssociatedObject.PasswordChar != '\0') return;

        // Announce significant changes (could be refined)
        if (Math.Abs(currentValue.Length - (_previousValue?.Length ?? 0)) > 5)
        {
            _ = AnnounceAsync($"Text updated, {currentValue.Length} characters", AccessibilityPriority.Low);
        }

        _previousValue = currentValue;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (AssociatedObject == null) return;

        // Handle Escape to clear field
        if (e.Key == Key.Escape)
        {
            AssociatedObject.Clear();
            _ = AnnounceAsync("Field cleared", AccessibilityPriority.Normal);
            e.Handled = true;
        }
    }
}

/// <summary>
/// Makes list controls accessible with proper announcements.
/// </summary>
public class AccessibleListBehavior : AccessibilityBehavior<ListBox>
{
    private object? _previousSelection;

    protected override void OnAttached()
    {
        base.OnAttached();
        
        if (AssociatedObject == null) return;

        AssociatedObject.SelectionChanged += OnSelectionChanged;
        AssociatedObject.GotFocus += OnGotFocus;

        // Set default automation properties
        if (string.IsNullOrEmpty(AutomationProperties.GetName(AssociatedObject)))
        {
            AutomationProperties.SetName(AssociatedObject, "List");
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        
        if (AssociatedObject == null) return;

        AssociatedObject.SelectionChanged -= OnSelectionChanged;
        AssociatedObject.GotFocus -= OnGotFocus;
    }

    private void OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (AssociatedObject == null) return;

        var itemCount = AssociatedObject.ItemCount;
        var listName = AutomationProperties.GetName(AssociatedObject) ?? "List";
        
        _ = AnnounceAsync($"{listName} with {itemCount} items", AccessibilityPriority.Low);
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AssociatedObject == null) return;

        var selectedItem = AssociatedObject.SelectedItem;
        if (selectedItem == null) return;

        var itemText = selectedItem.ToString() ?? "Unknown item";
        var selectedIndex = AssociatedObject.SelectedIndex + 1;
        var totalItems = AssociatedObject.ItemCount;

        if (selectedItem != _previousSelection)
        {
            _ = AnnounceAsync($"{itemText}, {selectedIndex} of {totalItems}", AccessibilityPriority.Normal);
            _previousSelection = selectedItem;
        }
    }
}

/// <summary>
/// Makes tab controls accessible with proper announcements.
/// </summary>
public class AccessibleTabBehavior : AccessibilityBehavior<TabControl>
{
    private int _previousIndex = -1;

    protected override void OnAttached()
    {
        base.OnAttached();
        
        if (AssociatedObject == null) return;

        AssociatedObject.SelectionChanged += OnSelectionChanged;

        // Set default automation properties
        if (string.IsNullOrEmpty(AutomationProperties.GetName(AssociatedObject)))
        {
            AutomationProperties.SetName(AssociatedObject, "Tab control");
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        
        if (AssociatedObject == null) return;

        AssociatedObject.SelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AssociatedObject == null) return;

        var selectedIndex = AssociatedObject.SelectedIndex;
        if (selectedIndex == _previousIndex) return;

        var selectedTab = AssociatedObject.SelectedItem as TabItem;
        var tabName = selectedTab?.Header?.ToString() ?? $"Tab {selectedIndex + 1}";
        var totalTabs = AssociatedObject.Items.Count;

        _ = AnnounceAsync($"{tabName} tab, {selectedIndex + 1} of {totalTabs}", AccessibilityPriority.Normal);
        _previousIndex = selectedIndex;
    }
}

/// <summary>
/// Announces content changes for screen reader users.
/// </summary>
public class LiveRegionBehavior : AccessibilityBehavior<Control>
{
    public static readonly StyledProperty<AriaLiveMode> LiveModeProperty =
        AvaloniaProperty.Register<LiveRegionBehavior, AriaLiveMode>(
            nameof(LiveMode), 
            AriaLiveMode.Polite);

    public AriaLiveMode LiveMode
    {
        get => GetValue(LiveModeProperty);
        set => SetValue(LiveModeProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        
        if (AssociatedObject == null) return;

        // Set up property change listening based on control type
        if (AssociatedObject is TextBlock textBlock)
        {
            textBlock.PropertyChanged += OnTextBlockPropertyChanged;
        }
        else if (AssociatedObject is ContentControl contentControl)
        {
            contentControl.PropertyChanged += OnContentControlPropertyChanged;
        }

        // Register as live region
        AccessibilityService?.SetAriaLiveAsync(AssociatedObject, LiveMode);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        
        if (AssociatedObject == null) return;

        if (AssociatedObject is TextBlock textBlock)
        {
            textBlock.PropertyChanged -= OnTextBlockPropertyChanged;
        }
        else if (AssociatedObject is ContentControl contentControl)
        {
            contentControl.PropertyChanged -= OnContentControlPropertyChanged;
        }
    }

    private void OnTextBlockPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBlock.TextProperty)
        {
            var newText = e.NewValue?.ToString();
            if (!string.IsNullOrEmpty(newText))
            {
                var priority = LiveMode == AriaLiveMode.Assertive 
                    ? AccessibilityPriority.High 
                    : AccessibilityPriority.Normal;
                _ = AnnounceAsync(newText, priority);
            }
        }
    }

    private void OnContentControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentControl.ContentProperty)
        {
            var newContent = e.NewValue?.ToString();
            if (!string.IsNullOrEmpty(newContent))
            {
                var priority = LiveMode == AriaLiveMode.Assertive 
                    ? AccessibilityPriority.High 
                    : AccessibilityPriority.Normal;
                _ = AnnounceAsync(newContent, priority);
            }
        }
    }
}
