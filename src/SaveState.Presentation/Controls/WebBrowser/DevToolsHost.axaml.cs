using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Controls.WebBrowser;

/// <summary>
/// DevTools docking mode.
/// </summary>
public enum DevToolsDockMode
{
    Bottom,
    Side,
    Undocked
}

/// <summary>
/// Control for hosting browser DevTools.
/// </summary>
public partial class DevToolsHost : UserControl
{
    public static readonly StyledProperty<DevToolsDockMode> DockModeProperty =
        AvaloniaProperty.Register<DevToolsHost, DevToolsDockMode>(nameof(DockMode), DevToolsDockMode.Bottom);

    public static readonly StyledProperty<bool> IsUndockedProperty =
        AvaloniaProperty.Register<DevToolsHost, bool>(nameof(IsUndocked));

    public DevToolsDockMode DockMode
    {
        get => GetValue(DockModeProperty);
        set => SetValue(DockModeProperty, value);
    }

    public bool IsUndocked
    {
        get => GetValue(IsUndockedProperty);
        private set => SetValue(IsUndockedProperty, value);
    }

    public event EventHandler? CloseRequested;
    public event EventHandler<DevToolsDockMode>? DockModeChanged;

    public DevToolsHost()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnToggleDockClick(object? sender, RoutedEventArgs e)
    {
        DockMode = DockMode switch
        {
            DevToolsDockMode.Bottom => DevToolsDockMode.Side,
            DevToolsDockMode.Side => DevToolsDockMode.Bottom,
            _ => DevToolsDockMode.Bottom
        };

        IsUndocked = false;
        DockModeChanged?.Invoke(this, DockMode);
    }

    private void OnUndockClick(object? sender, RoutedEventArgs e)
    {
        IsUndocked = true;
        DockMode = DevToolsDockMode.Undocked;
        DockModeChanged?.Invoke(this, DockMode);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
