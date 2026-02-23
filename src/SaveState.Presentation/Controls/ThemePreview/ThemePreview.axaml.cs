using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Core.Theme.Models;

namespace SaveState.Presentation.Controls.ThemePreview;

/// <summary>
/// A control that displays a preview of a theme with various UI components.
/// </summary>
public partial class ThemePreview : UserControl
{
    public static readonly StyledProperty<ThemeDefinition?> ThemeProperty =
        AvaloniaProperty.Register<ThemePreview, ThemeDefinition?>(nameof(Theme));

    public static readonly StyledProperty<bool> ShowButtonsProperty =
        AvaloniaProperty.Register<ThemePreview, bool>(nameof(ShowButtons), defaultValue: true);

    public static readonly StyledProperty<bool> ShowCardsProperty =
        AvaloniaProperty.Register<ThemePreview, bool>(nameof(ShowCards), defaultValue: true);

    public static readonly StyledProperty<bool> ShowInputsProperty =
        AvaloniaProperty.Register<ThemePreview, bool>(nameof(ShowInputs), defaultValue: true);

    public static readonly StyledProperty<bool> ShowAlertsProperty =
        AvaloniaProperty.Register<ThemePreview, bool>(nameof(ShowAlerts), defaultValue: true);

    public static readonly StyledProperty<bool> ShowTypographyProperty =
        AvaloniaProperty.Register<ThemePreview, bool>(nameof(ShowTypography), defaultValue: true);

    public static readonly StyledProperty<bool> ShowColorPaletteProperty =
        AvaloniaProperty.Register<ThemePreview, bool>(nameof(ShowColorPalette), defaultValue: true);

    /// <summary>
    /// Gets or sets the theme to preview.
    /// </summary>
    public ThemeDefinition? Theme
    {
        get => GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the buttons section.
    /// </summary>
    public bool ShowButtons
    {
        get => GetValue(ShowButtonsProperty);
        set => SetValue(ShowButtonsProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the cards section.
    /// </summary>
    public bool ShowCards
    {
        get => GetValue(ShowCardsProperty);
        set => SetValue(ShowCardsProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the inputs section.
    /// </summary>
    public bool ShowInputs
    {
        get => GetValue(ShowInputsProperty);
        set => SetValue(ShowInputsProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the alerts section.
    /// </summary>
    public bool ShowAlerts
    {
        get => GetValue(ShowAlertsProperty);
        set => SetValue(ShowAlertsProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the typography section.
    /// </summary>
    public bool ShowTypography
    {
        get => GetValue(ShowTypographyProperty);
        set => SetValue(ShowTypographyProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the color palette section.
    /// </summary>
    public bool ShowColorPalette
    {
        get => GetValue(ShowColorPaletteProperty);
        set => SetValue(ShowColorPaletteProperty, value);
    }

    public ThemePreview()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ThemeProperty && Theme != null)
        {
            UpdateThemeResources();
        }
    }

    private void UpdateThemeResources()
    {
        if (Theme == null) return;

        // Apply theme colors to local resources
        var resources = Resources;

        resources["PrimaryBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.Primary));
        resources["OnPrimaryBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.OnPrimary));
        resources["PrimaryContainerBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.PrimaryContainer));
        resources["OnPrimaryContainerBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.OnPrimaryContainer));

        resources["SecondaryBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.Secondary));
        resources["OnSecondaryBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.OnSecondary));
        resources["SecondaryContainerBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.SecondaryContainer));
        resources["OnSecondaryContainerBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.OnSecondaryContainer));

        resources["TertiaryBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.Tertiary));
        resources["OnTertiaryBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.OnTertiary));
        resources["TertiaryContainerBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.TertiaryContainer));
        resources["OnTertiaryContainerBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.OnTertiaryContainer));

        resources["ErrorBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.Error));
        resources["OnErrorBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.OnError));
        resources["ErrorContainerBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.ErrorContainer));
        resources["OnErrorContainerBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.OnErrorContainer));

        resources["BackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.Background));
        resources["OnBackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.OnBackground));
        resources["SurfaceBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.Surface));
        resources["OnSurfaceBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.OnSurface));
        resources["SurfaceVariantBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.SurfaceVariant));
        resources["OnSurfaceVariantBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.OnSurfaceVariant));

        resources["OutlineBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.Outline));
        resources["OutlineVariantBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.OutlineVariant));

        resources["InverseSurfaceBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.InverseSurface));
        resources["InverseOnSurfaceBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.InverseOnSurface));
        resources["InversePrimaryBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.InversePrimary));

        resources["SurfaceContainerLowestBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.SurfaceContainerLowest));
        resources["SurfaceContainerLowBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.SurfaceContainerLow));
        resources["SurfaceContainerBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.SurfaceContainer));
        resources["SurfaceContainerHighBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.SurfaceContainerHigh));
        resources["SurfaceContainerHighestBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.SurfaceContainerHighest));

        resources["GlassBackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.GlassBackground));
        resources["GlassBorderBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(Theme.Colors.GlassBorder));

        // Additional utility brushes
        resources["SuccessBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#4CAF50"));
        resources["SuccessContainerBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E8F5E9"));
        resources["OnSuccessContainerBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1B5E20"));
    }
}
