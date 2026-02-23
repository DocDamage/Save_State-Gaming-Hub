using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace SaveState.Presentation.Behaviors;

#region Ripple Behavior

/// <summary>
/// Adds a material design ripple effect to buttons on click.
/// 
/// Usage:
/// <code>
/// &lt;Button Content="Click Me"&gt;
///     &lt;i:Interaction.Behaviors&gt;
///         &lt;behaviors:RippleBehavior RippleColor="White" RippleOpacity="0.3" /&gt;
///     &lt;/i:Interaction.Behaviors&gt;
/// &lt;/Button&gt;
/// </code>
/// </summary>
public class RippleBehavior : Behavior<Button>
{
    public static readonly StyledProperty<Color> RippleColorProperty =
        AvaloniaProperty.Register<RippleBehavior, Color>(
            nameof(RippleColor),
            Colors.White);

    public static readonly StyledProperty<double> RippleOpacityProperty =
        AvaloniaProperty.Register<RippleBehavior, double>(
            nameof(RippleOpacity),
            0.3);

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<RippleBehavior, TimeSpan>(
            nameof(Duration),
            TimeSpan.FromMilliseconds(600));

    public Color RippleColor
    {
        get => GetValue(RippleColorProperty);
        set => SetValue(RippleColorProperty, value);
    }

    public double RippleOpacity
    {
        get => GetValue(RippleOpacityProperty);
        set => SetValue(RippleOpacityProperty, value);
    }

    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    private Panel? _container;
    private readonly SerialDisposable _rippleDisposable = new();

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not null)
        {
            AssociatedObject.Click += OnClick;
            EnsureContainer();
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject is not null)
        {
            AssociatedObject.Click -= OnClick;
        }

        _rippleDisposable.Dispose();
    }

    private void EnsureContainer()
    {
        if (AssociatedObject is null) return;

        // Find or create a container panel
        if (AssociatedObject.Content is Panel panel)
        {
            _container = panel;
        }
        else
        {
            // Wrap content in a grid if not already a panel
            var grid = new Grid();
            var content = AssociatedObject.Content;
            AssociatedObject.Content = null;
            grid.Children.Add(content as Control ?? new ContentControl { Content = content });
            AssociatedObject.Content = grid;
            _container = grid;
        }

        // Ensure clip to bounds for clean ripple
        if (_container is not null)
        {
            _container.ClipToBounds = true;
        }
    }

    private async void OnClick(object? sender, RoutedEventArgs e)
    {
        if (_container is null || AssociatedObject is null) return;

        // Get click position relative to the button
        if (e.Source is not InputElement element) return;

        var position = e.GetPosition(_container);
        var maxRadius = Math.Max(
            Math.Max(position.X, _container.Bounds.Width - position.X),
            Math.Max(position.Y, _container.Bounds.Height - position.Y)
        ) * 1.5;

        // Create ripple ellipse
        var ripple = new Ellipse
        {
            Width = 0,
            Height = 0,
            Fill = new SolidColorBrush(
                Color.FromArgb(
                    (byte)(RippleOpacity * 255),
                    RippleColor.R,
                    RippleColor.G,
                    RippleColor.B)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(position.X, position.Y, 0, 0),
            RenderTransform = new TranslateTransform(-0, -0)
        };

        _container.Children.Add(ripple);

        // Create and run animation
        var animation = new Animation
        {
            Duration = Duration,
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(WidthProperty, 0.0),
                        new Setter(HeightProperty, 0.0),
                        new Setter(OpacityProperty, RippleOpacity)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(WidthProperty, maxRadius * 2),
                        new Setter(HeightProperty, maxRadius * 2),
                        new Setter(OpacityProperty, 0.0)
                    }
                }
            }
        };

        // Center the ripple on click point
        ripple.RenderTransform = new TranslateTransform(-maxRadius, -maxRadius);

        try
        {
            await animation.RunAsync(ripple, System.Threading.CancellationToken.None);
        }
        finally
        {
            _container.Children.Remove(ripple);
        }
    }
}

#endregion

#region Scale On Press Behavior

/// <summary>
/// Scales a button down when pressed and back up when released.
/// 
/// Usage:
/// <code>
/// &lt;Button Content="Press Me"&gt;
///     &lt;i:Interaction.Behaviors&gt;
///         &lt;behaviors:ScaleOnPressBehavior ScaleFactor="0.95" /&gt;
///     &lt;/i:Interaction.Behaviors&gt;
/// &lt;/Button&gt;
/// </code>
/// </summary>
public class ScaleOnPressBehavior : Behavior<Button>
{
    public static readonly StyledProperty<double> ScaleFactorProperty =
        AvaloniaProperty.Register<ScaleOnPressBehavior, double>(
            nameof(ScaleFactor),
            0.95);

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<ScaleOnPressBehavior, TimeSpan>(
            nameof(Duration),
            TimeSpan.FromMilliseconds(100));

    public double ScaleFactor
    {
        get => GetValue(ScaleFactorProperty);
        set => SetValue(ScaleFactorProperty, value);
    }

    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    private ScaleTransform? _scaleTransform;

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is null) return;

        AssociatedObject.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        _scaleTransform = new ScaleTransform(1, 1);
        AssociatedObject.RenderTransform = _scaleTransform;

        AssociatedObject.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AssociatedObject.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        AssociatedObject.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerReleased, RoutingStrategies.Tunnel);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject is null) return;

        AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        AssociatedObject.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        AssociatedObject.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerReleased);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        AnimateTo(ScaleFactor);
    }

    private void OnPointerReleased(object? sender, RoutedEventArgs e)
    {
        AnimateTo(1.0);
    }

    private void AnimateTo(double scale)
    {
        if (_scaleTransform is null) return;

        var animation = new Animation
        {
            Duration = Duration,
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(ScaleTransform.ScaleXProperty, _scaleTransform.ScaleX),
                        new Setter(ScaleTransform.ScaleYProperty, _scaleTransform.ScaleY)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(ScaleTransform.ScaleXProperty, scale),
                        new Setter(ScaleTransform.ScaleYProperty, scale)
                    }
                }
            }
        };

        animation.RunAsync(_scaleTransform, System.Threading.CancellationToken.None);
    }
}

#endregion

#region Hover Glow Behavior

/// <summary>
/// Adds a glow effect when the mouse hovers over a control.
/// 
/// Usage:
/// <code>
/// &lt;Border&gt;
///     &lt;i:Interaction.Behaviors&gt;
///         &lt;behaviors:HoverGlowBehavior GlowColor="#2196F3" GlowBlur="20" /&gt;
///     &lt;/i:Interaction.Behaviors&gt;
/// &lt;/Border&gt;
/// </code>
/// </summary>
public class HoverGlowBehavior : Behavior<Control>
{
    public static readonly StyledProperty<Color> GlowColorProperty =
        AvaloniaProperty.Register<HoverGlowBehavior, Color>(
            nameof(GlowColor),
            Color.Parse("#2196F3"));

    public static readonly StyledProperty<double> GlowBlurProperty =
        AvaloniaProperty.Register<HoverGlowBehavior, double>(
            nameof(GlowBlur),
            20);

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<HoverGlowBehavior, TimeSpan>(
            nameof(Duration),
            TimeSpan.FromMilliseconds(200));

    public Color GlowColor
    {
        get => GetValue(GlowColorProperty);
        set => SetValue(GlowColorProperty, value);
    }

    public double GlowBlur
    {
        get => GetValue(GlowBlurProperty);
        set => SetValue(GlowBlurProperty, value);
    }

    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    private BoxShadows _originalShadow;

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is null) return;

        AssociatedObject.PointerEntered += OnPointerEntered;
        AssociatedObject.PointerExited += OnPointerExited;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject is null) return;

        AssociatedObject.PointerEntered -= OnPointerEntered;
        AssociatedObject.PointerExited -= OnPointerExited;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (AssociatedObject is null) return;

        // Store original shadow
        _originalShadow = AssociatedObject.BoxShadow;

        // Apply glow
        var glowShadow = new BoxShadow
        {
            Color = new Color(100, GlowColor.R, GlowColor.G, GlowColor.B),
            Blur = GlowBlur,
            Spread = 0,
            OffsetX = 0,
            OffsetY = 0
        };

        AssociatedObject.BoxShadow = new BoxShadows(glowShadow);
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (AssociatedObject is null) return;

        // Restore original shadow
        AssociatedObject.BoxShadow = _originalShadow;
    }
}

#endregion

#region Magnetic Button Behavior

/// <summary>
/// Makes a button slightly "magnetic" by moving towards the cursor on hover.
/// 
/// Usage:
/// <code>
/// &lt;Button Content="Magnetic"&gt;
///     &lt;i:Interaction.Behaviors&gt;
///         &lt;behaviors:MagneticButtonBehavior Strength="15" /&gt;
///     &lt;/i:Interaction.Behaviors&gt;
/// &lt;/Button&gt;
/// </code>
/// </summary>
public class MagneticButtonBehavior : Behavior<Button>
{
    public static readonly StyledProperty<double> StrengthProperty =
        AvaloniaProperty.Register<MagneticButtonBehavior, double>(
            nameof(Strength),
            15);

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<MagneticButtonBehavior, TimeSpan>(
            nameof(Duration),
            TimeSpan.FromMilliseconds(150));

    public double Strength
    {
        get => GetValue(StrengthProperty);
        set => SetValue(StrengthProperty, value);
    }

    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    private TranslateTransform? _translateTransform;
    private readonly SerialDisposable _moveDisposable = new();

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is null) return;

        _translateTransform = new TranslateTransform(0, 0);
        AssociatedObject.RenderTransform = _translateTransform;

        AssociatedObject.PointerEntered += OnPointerEntered;
        AssociatedObject.PointerExited += OnPointerExited;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        _moveDisposable.Dispose();

        if (AssociatedObject is null) return;

        AssociatedObject.PointerEntered -= OnPointerEntered;
        AssociatedObject.PointerExited -= OnPointerExited;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (AssociatedObject is null) return;

        AssociatedObject.PointerMoved += OnPointerMoved;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (AssociatedObject is null) return;

        AssociatedObject.PointerMoved -= OnPointerMoved;
        AnimateTo(0, 0);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (AssociatedObject is null) return;

        var position = e.GetPosition(AssociatedObject);
        var centerX = AssociatedObject.Bounds.Width / 2;
        var centerY = AssociatedObject.Bounds.Height / 2;

        // Calculate offset from center (normalized -1 to 1)
        var offsetX = (position.X - centerX) / centerX;
        var offsetY = (position.Y - centerY) / centerY;

        // Apply magnetic effect
        var moveX = offsetX * Strength;
        var moveY = offsetY * Strength;

        AnimateTo(moveX, moveY);
    }

    private void AnimateTo(double x, double y)
    {
        if (_translateTransform is null) return;

        var animation = new Animation
        {
            Duration = Duration,
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(TranslateTransform.XProperty, _translateTransform.X),
                        new Setter(TranslateTransform.YProperty, _translateTransform.Y)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(TranslateTransform.XProperty, x),
                        new Setter(TranslateTransform.YProperty, y)
                    }
                }
            }
        };

        animation.RunAsync(_translateTransform, System.Threading.CancellationToken.None);
    }
}

#endregion

#region Focus Animation Behavior

/// <summary>
/// Animates a control when it receives focus.
/// 
/// Usage:
/// <code>
/// &lt;TextBox&gt;
///     &lt;i:Interaction.Behaviors&gt;
///         &lt;behaviors:FocusAnimationBehavior Scale="1.02" /&gt;
///     &lt;/i:Interaction.Behaviors&gt;
/// &lt;/TextBox&gt;
/// </code>
/// </summary>
public class FocusAnimationBehavior : Behavior<Control>
{
    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<FocusAnimationBehavior, double>(
            nameof(Scale),
            1.02);

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<FocusAnimationBehavior, TimeSpan>(
            nameof(Duration),
            TimeSpan.FromMilliseconds(200));

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    private ScaleTransform? _scaleTransform;

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is null) return;

        AssociatedObject.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        _scaleTransform = new ScaleTransform(1, 1);
        AssociatedObject.RenderTransform = _scaleTransform;

        AssociatedObject.GotFocus += OnGotFocus;
        AssociatedObject.LostFocus += OnLostFocus;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject is null) return;

        AssociatedObject.GotFocus -= OnGotFocus;
        AssociatedObject.LostFocus -= OnLostFocus;
    }

    private void OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        AnimateTo(Scale);
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        AnimateTo(1.0);
    }

    private void AnimateTo(double scale)
    {
        if (_scaleTransform is null) return;

        var animation = new Animation
        {
            Duration = Duration,
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(ScaleTransform.ScaleXProperty, _scaleTransform.ScaleX),
                        new Setter(ScaleTransform.ScaleYProperty, _scaleTransform.ScaleY)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(ScaleTransform.ScaleXProperty, scale),
                        new Setter(ScaleTransform.ScaleYProperty, scale)
                    }
                }
            }
        };

        animation.RunAsync(_scaleTransform, System.Threading.CancellationToken.None);
    }
}

#endregion

#region Tilt Behavior

/// <summary>
/// Adds a 3D tilt effect to a control based on mouse position.
/// 
/// Usage:
/// <code>
/// &lt;Border&gt;
///     &lt;i:Interaction.Behaviors&gt;
///         &lt;behaviors:TiltBehavior MaxTilt="10" /&gt;
///     &lt;/i:Interaction.Behaviors&gt;
/// &lt;/Border&gt;
/// </code>
/// </summary>
public class TiltBehavior : Behavior<Control>
{
    public static readonly StyledProperty<double> MaxTiltProperty =
        AvaloniaProperty.Register<TiltBehavior, double>(
            nameof(MaxTilt),
            10);

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<TiltBehavior, TimeSpan>(
            nameof(Duration),
            TimeSpan.FromMilliseconds(150));

    public double MaxTilt
    {
        get => GetValue(MaxTiltProperty);
        set => SetValue(MaxTiltProperty, value);
    }

    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    private Rotate3DTransform? _rotateTransform;

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is null) return;

        AssociatedObject.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        _rotateTransform = new Rotate3DTransform();
        AssociatedObject.RenderTransform = _rotateTransform;

        AssociatedObject.PointerMoved += OnPointerMoved;
        AssociatedObject.PointerExited += OnPointerExited;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject is null) return;

        AssociatedObject.PointerMoved -= OnPointerMoved;
        AssociatedObject.PointerExited -= OnPointerExited;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (AssociatedObject is null || _rotateTransform is null) return;

        var position = e.GetPosition(AssociatedObject);
        var centerX = AssociatedObject.Bounds.Width / 2;
        var centerY = AssociatedObject.Bounds.Height / 2;

        // Calculate tilt angles
        var percentX = (position.X - centerX) / centerX;
        var percentY = (position.Y - centerY) / centerY;

        var angleX = -percentY * MaxTilt;
        var angleY = percentX * MaxTilt;

        AnimateTo(angleX, angleY);
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        AnimateTo(0, 0);
    }

    private void AnimateTo(double angleX, double angleY)
    {
        if (_rotateTransform is null) return;

        var animation = new Animation
        {
            Duration = Duration,
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(Rotate3DTransform.AngleXProperty, _rotateTransform.AngleX),
                        new Setter(Rotate3DTransform.AngleYProperty, _rotateTransform.AngleY)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(Rotate3DTransform.AngleXProperty, angleX),
                        new Setter(Rotate3DTransform.AngleYProperty, angleY)
                    }
                }
            }
        };

        animation.RunAsync(_rotateTransform, System.Threading.CancellationToken.None);
    }
}

#endregion

#region Rotate3DTransform Helper Class

/// <summary>
/// A transform that provides 3D rotation capabilities.
/// </summary>
public class Rotate3DTransform : Transform
{
    public static readonly StyledProperty<double> AngleXProperty =
        AvaloniaProperty.Register<Rotate3DTransform, double>(nameof(AngleX));

    public static readonly StyledProperty<double> AngleYProperty =
        AvaloniaProperty.Register<Rotate3DTransform, double>(nameof(AngleY));

    public static readonly StyledProperty<double> AngleZProperty =
        AvaloniaProperty.Register<Rotate3DTransform, double>(nameof(AngleZ));

    public double AngleX
    {
        get => GetValue(AngleXProperty);
        set => SetValue(AngleXProperty, value);
    }

    public double AngleY
    {
        get => GetValue(AngleYProperty);
        set => SetValue(AngleYProperty, value);
    }

    public double AngleZ
    {
        get => GetValue(AngleZProperty);
        set => SetValue(AngleZProperty, value);
    }

    public override Matrix Value => Matrix.Identity;
}

#endregion
