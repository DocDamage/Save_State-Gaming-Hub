using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;

namespace SaveState.Presentation.Controls.Loading;

/// <summary>
/// A container control that displays a skeleton loading overlay when IsLoading is true.
/// 
/// Usage:
/// <code>
/// &lt;local:SkeletonContainer IsLoading="{Binding IsLoading}" LoadingMessage="Loading games..."&gt;
///     &lt;views:GameListView /&gt;
/// &lt;/local:SkeletonContainer&gt;
/// </code>
/// </summary>
public partial class SkeletonContainer : UserControl
{
    /// <summary>
    /// Defines the <see cref="IsLoading"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<SkeletonContainer, bool>(
            nameof(IsLoading),
            false);

    /// <summary>
    /// Defines the <see cref="LoadingMessage"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> LoadingMessageProperty =
        AvaloniaProperty.Register<SkeletonContainer, string?>(
            nameof(LoadingMessage));

    /// <summary>
    /// Defines the <see cref="ShowLoadingMessage"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowLoadingMessageProperty =
        AvaloniaProperty.Register<SkeletonContainer, bool>(
            nameof(ShowLoadingMessage),
            false);

    /// <summary>
    /// Defines the <see cref="SkeletonBackground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush> SkeletonBackgroundProperty =
        AvaloniaProperty.Register<SkeletonContainer, IBrush>(
            nameof(SkeletonBackground),
            new SolidColorBrush(Color.Parse("#F0F0F0")));

    /// <summary>
    /// Defines the <see cref="ShimmerBrush"/> property.
    /// </summary>
    public static readonly StyledProperty<LinearGradientBrush> ShimmerBrushProperty =
        AvaloniaProperty.Register<SkeletonContainer, LinearGradientBrush>(
            nameof(ShimmerBrush),
            CreateDefaultShimmerBrush());

    /// <summary>
    /// Defines the <see cref="EnableShimmerAnimation"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableShimmerAnimationProperty =
        AvaloniaProperty.Register<SkeletonContainer, bool>(
            nameof(EnableShimmerAnimation),
            true);

    /// <summary>
    /// Gets or sets whether the skeleton loading state is active.
    /// </summary>
    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    /// <summary>
    /// Gets or sets the loading message to display.
    /// </summary>
    public string? LoadingMessage
    {
        get => GetValue(LoadingMessageProperty);
        set => SetValue(LoadingMessageProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the loading message.
    /// </summary>
    public bool ShowLoadingMessage
    {
        get => GetValue(ShowLoadingMessageProperty);
        set => SetValue(ShowLoadingMessageProperty, value);
    }

    /// <summary>
    /// Gets or sets the background brush for the skeleton overlay.
    /// </summary>
    public IBrush SkeletonBackground
    {
        get => GetValue(SkeletonBackgroundProperty);
        set => SetValue(SkeletonBackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the shimmer gradient brush.
    /// </summary>
    public LinearGradientBrush ShimmerBrush
    {
        get => GetValue(ShimmerBrushProperty);
        set => SetValue(ShimmerBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the shimmer animation is enabled.
    /// </summary>
    public bool EnableShimmerAnimation
    {
        get => GetValue(EnableShimmerAnimationProperty);
        set => SetValue(EnableShimmerAnimationProperty, value);
    }

    private Animation? _shimmerAnimation;
    private CancellationTokenSource? _animationCts;
    private TranslateTransform? _shimmerTransform;

    public SkeletonContainer()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        SetupShimmerAnimation();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsLoadingProperty)
        {
            if (IsLoading)
            {
                StartShimmerAnimation();
            }
            else
            {
                StopShimmerAnimation();
            }
        }
        else if (change.Property == LoadingMessageProperty)
        {
            ShowLoadingMessage = !string.IsNullOrEmpty(LoadingMessage);
        }
        else if (change.Property == EnableShimmerAnimationProperty)
        {
            if (IsLoading)
            {
                if (EnableShimmerAnimation)
                {
                    StartShimmerAnimation();
                }
                else
                {
                    StopShimmerAnimation();
                }
            }
        }
    }

    private void SetupShimmerAnimation()
    {
        if (_shimmerTransform is null) return;

        _shimmerAnimation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(1500),
            IterationCount = IterationCount.Infinite,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(TranslateTransform.XProperty, -200.0)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(TranslateTransform.XProperty, 600.0)
                    }
                }
            }
        };
    }

    private void StartShimmerAnimation()
    {
        if (!EnableShimmerAnimation || _shimmerTransform is null) return;

        StopShimmerAnimation();

        _animationCts = new CancellationTokenSource();
        _shimmerAnimation?.RunAsync(_shimmerTransform, _animationCts.Token);
    }

    private void StopShimmerAnimation()
    {
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        _animationCts = null;
    }

    private static LinearGradientBrush CreateDefaultShimmerBrush()
    {
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Absolute),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Absolute),
            GradientStops =
            {
                new GradientStop(Color.Parse("#E8E8E8"), 0.0),
                new GradientStop(Color.Parse("#F5F5F5"), 0.3),
                new GradientStop(Color.Parse("#FFFFFF"), 0.5),
                new GradientStop(Color.Parse("#F5F5F5"), 0.7),
                new GradientStop(Color.Parse("#E8E8E8"), 1.0)
            }
        };
    }
}
