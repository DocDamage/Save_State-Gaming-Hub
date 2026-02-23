using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace SaveState.EndToEndTests.Infrastructure;

/// <summary>
/// Extension methods for finding and interacting with Avalonia controls in tests.
/// </summary>
public static class ControlExtensions
{
    /// <summary>
    /// Finds a control by name within the visual tree.
    /// </summary>
    public static T? FindControl<T>(this Visual parent, string name) where T : class, Visual
    {
        return parent.GetVisualDescendants()
            .OfType<T>()
            .FirstOrDefault(x => x.Name == name);
    }

    /// <summary>
    /// Finds a control by name within the logical tree.
    /// </summary>
    public static T? FindLogicalControl<T>(this ILogical parent, string name) where T : class, ILogical
    {
        return parent.GetLogicalDescendants()
            .OfType<T>()
            .FirstOrDefault(x => (x as Visual)?.Name == name);
    }

    /// <summary>
    /// Finds all controls of a specific type within the visual tree.
    /// </summary>
    public static IEnumerable<T> FindControls<T>(this Visual parent) where T : class, Visual
    {
        return parent.GetVisualDescendants().OfType<T>();
    }

    /// <summary>
    /// Finds a control by its automation ID/name.
    /// </summary>
    public static T? FindByAutomationId<T>(this Visual parent, string automationId) where T : class, Visual
    {
        return parent.GetVisualDescendants()
            .OfType<T>()
            .FirstOrDefault(x => 
            {
                if (x is InputElement inputElement)
                    return AutomationProperties.GetName(inputElement) == automationId;
                return false;
            });
    }

    /// <summary>
    /// Simulates a click on a button.
    /// </summary>
    public static void Click(this Button button)
    {
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    }

    /// <summary>
    /// Simulates a click on a button with async support.
    /// </summary>
    public static async Task ClickAsync(this Button button, CancellationToken cancellationToken = default)
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        });
        await Task.Delay(50, cancellationToken);
    }

    /// <summary>
    /// Gets the window containing this control.
    /// </summary>
    public static Window? GetWindow(this Visual control)
    {
        var current = control;
        while (current != null)
        {
            if (current is Window window)
                return window;
            current = current.GetVisualParent();
        }
        return null;
    }

    /// <summary>
    /// Waits for the control to be fully loaded and arranged.
    /// </summary>
    public static async Task WaitForLayoutAsync(this Visual control, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        void OnLayoutUpdated(object? sender, EventArgs e)
        {
            tcs.TrySetResult(true);
        }

        control.LayoutUpdated += OnLayoutUpdated;
        
        try
        {
            // Trigger layout
            control.InvalidateMeasure();
            control.InvalidateArrange();
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            
            await tcs.Task;
        }
        finally
        {
            control.LayoutUpdated -= OnLayoutUpdated;
        }
    }

    /// <summary>
    /// Checks if a control is visible and enabled for interaction.
    /// </summary>
    public static bool IsInteractive(this Visual control)
    {
        if (!control.IsVisible)
            return false;

        if (control is InputElement inputElement)
            return inputElement.IsEnabled;

        return true;
    }

    /// <summary>
    /// Finds a TextBox by its watermark text.
    /// </summary>
    public static TextBox? FindTextBoxByWatermark(this Visual parent, string watermark)
    {
        return parent.GetVisualDescendants()
            .OfType<TextBox>()
            .FirstOrDefault(x => x.Watermark?.ToString() == watermark);
    }

    /// <summary>
    /// Finds a Button by its content text.
    /// </summary>
    public static Button? FindButtonByContent(this Visual parent, string content)
    {
        return parent.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(x => 
            {
                var buttonContent = x.Content?.ToString();
                return buttonContent?.Equals(content, StringComparison.OrdinalIgnoreCase) == true;
            });
    }

    /// <summary>
    /// Gets all items from an ItemsControl.
    /// </summary>
    public static IEnumerable<object?> GetAllItems(this ItemsControl itemsControl)
    {
        if (itemsControl.ItemsSource != null)
            return itemsControl.ItemsSource.Cast<object?>();
        
        return itemsControl.Items.Cast<object?>();
    }

    /// <summary>
    /// Finds a ListBoxItem by its content.
    /// </summary>
    public static ListBoxItem? FindItemByContent(this ListBox listBox, string content)
    {
        return listBox.GetLogicalDescendants()
            .OfType<ListBoxItem>()
            .FirstOrDefault(x => x.Content?.ToString()?.Contains(content, StringComparison.OrdinalIgnoreCase) == true);
    }
}
