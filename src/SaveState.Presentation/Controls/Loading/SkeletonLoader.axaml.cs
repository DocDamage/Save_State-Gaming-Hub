using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;

namespace SaveState.Presentation.Controls.Loading;

/// <summary>
/// A skeleton loading placeholder control that displays an animated shimmer effect.
/// 
/// Usage:
/// <code>
/// &lt;local:SkeletonLoader Width="200" Height="20" CornerRadius="4" /&gt;
/// </code>
/// 
/// Or as a container layout:
/// <code>
/// &lt;local:SkeletonContainer&gt;
///     &lt;local:SkeletonLoader Height="20" /&gt;
///     &lt;local:SkeletonLoader Height="60" /&gt;
/// &lt;/local:SkeletonContainer&gt;
/// </code>
/// </summary>
public partial class SkeletonLoader : UserControl
{
    /// <summary>
    /// Defines the <see cref="CornerRadius"/> property.
    /// </summary>
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<SkeletonLoader, CornerRadius>(
            nameof(CornerRadius),
            new CornerRadius(4));

    /// <summary>
    /// Defines the <see cref="IsShimmerEnabled"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsShimmerEnabledProperty =
        AvaloniaProperty.Register<SkeletonLoader, bool>(
            nameof(IsShimmerEnabled),
            true);

    /// <summary>
    /// Defines the <see cref="ShimmerColor"/> property.
    /// </summary>
    public static readonly StyledProperty<Color> ShimmerColorProperty =
        AvaloniaProperty.Register<SkeletonLoader, Color>(
            nameof(ShimmerColor),
            Colors.White);

    /// <summary>
    /// Defines the <see cref="BaseColor"/> property.
    /// </summary>
    public static readonly StyledProperty<Color> BaseColorProperty =
        AvaloniaProperty.Register<SkeletonLoader, Color>(
            nameof(BaseColor),
            Color.Parse("#E0E0E0"));

    /// <summary>
    /// Gets or sets the corner radius of the skeleton loader.
    /// </summary>
    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the shimmer animation is enabled.
    /// </summary>
    public bool IsShimmerEnabled
    {
        get => GetValue(IsShimmerEnabledProperty);
        set => SetValue(IsShimmerEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets the shimmer highlight color.
    /// </summary>
    public Color ShimmerColor
    {
        get => GetValue(ShimmerColorProperty);
        set => SetValue(ShimmerColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the base background color.
    /// </summary>
    public Color BaseColor
    {
        get => GetValue(BaseColorProperty);
        set => SetValue(BaseColorProperty, value);
    }

    public SkeletonLoader()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateShimmerState();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsShimmerEnabledProperty)
        {
            UpdateShimmerState();
        }
        else if (change.Property == BaseColorProperty)
        {
            if (RootBorder is not null)
            {
                RootBorder.Background = new SolidColorBrush(BaseColor);
            }
        }
    }

    private void UpdateShimmerState()
    {
        if (ShimmerRectangle is null) return;

        if (IsShimmerEnabled)
        {
            ShimmerRectangle.IsVisible = true;
        }
        else
        {
            ShimmerRectangle.IsVisible = false;
        }
    }
}
