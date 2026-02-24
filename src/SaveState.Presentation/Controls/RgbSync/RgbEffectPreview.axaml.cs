using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SaveState.Core.RgbSync.Models;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.Controls.RgbSync;

public partial class RgbEffectPreview : UserControl
{
    private DispatcherTimer? _animationTimer;
    private int _currentFrame;
    private List<RgbColor> _frameColors = new();

    public static new readonly StyledProperty<RgbEffect?> EffectProperty =
        AvaloniaProperty.Register<RgbEffectPreview, RgbEffect?>(nameof(Effect));

    public static readonly StyledProperty<bool> IsPlayingProperty =
        AvaloniaProperty.Register<RgbEffectPreview, bool>(nameof(IsPlaying));

    public static readonly StyledProperty<int> CurrentFrameProperty =
        AvaloniaProperty.Register<RgbEffectPreview, int>(nameof(CurrentFrame));

    public new RgbEffect? Effect
    {
        get => GetValue(EffectProperty);
        set => SetValue(EffectProperty, value);
    }

    public bool IsPlaying
    {
        get => GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public int CurrentFrame
    {
        get => GetValue(CurrentFrameProperty);
        private set => SetValue(CurrentFrameProperty, value);
    }

    public RgbEffectPreview()
    {
        InitializeComponent();
        InitializeAnimation();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeAnimation()
    {
        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _animationTimer.Tick += OnAnimationTick;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsPlayingProperty)
        {
            if (IsPlaying)
            {
                _animationTimer?.Start();
            }
            else
            {
                _animationTimer?.Stop();
            }
        }
        else if (change.Property == EffectProperty)
        {
            _currentFrame = 0;
            UpdatePreview();
        }
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        _currentFrame++;
        CurrentFrame = _currentFrame;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (Effect == null) return;

        // Calculate colors based on effect type and current frame
        var colors = CalculateFrameColors(Effect, _currentFrame);
        _frameColors = colors.ToList();

        // Update the visual representation
        // In a full implementation, this would update the LED borders
    }

    private IEnumerable<RgbColor> CalculateFrameColors(RgbEffect effect, int frame)
    {
        var colors = new List<RgbColor>();
        var baseColors = effect.Colors.Count > 0 ? effect.Colors : new List<RgbColor> { RgbColor.White };

        switch (effect.Type)
        {
            case RgbEffectType.Static:
                // All LEDs same color
                for (int i = 0; i < 75; i++) // 75 LEDs in preview
                {
                    colors.Add(baseColors[0]);
                }
                break;

            case RgbEffectType.Breathing:
                // Pulsing brightness
                var brightness = (Math.Sin(frame * 0.1 * effect.Speed) + 1) / 2;
                for (int i = 0; i < 75; i++)
                {
                    colors.Add(new RgbColor(
                        (byte)(baseColors[0].R * brightness),
                        (byte)(baseColors[0].G * brightness),
                        (byte)(baseColors[0].B * brightness)
                    ));
                }
                break;

            case RgbEffectType.Rainbow:
                // Cycling rainbow colors
                for (int i = 0; i < 75; i++)
                {
                    var hue = ((frame * effect.Speed * 2) + (i * 5)) % 360;
                    colors.Add(HslToRgb(hue, 1.0, 0.5));
                }
                break;

            case RgbEffectType.Wave:
                // Moving wave pattern
                for (int i = 0; i < 75; i++)
                {
                    var offset = (frame * effect.Speed * 3 + i * 10) % 360;
                    var wave = (Math.Sin(offset * Math.PI / 180) + 1) / 2;
                    colors.Add(new RgbColor(
                        (byte)(baseColors[0].R * wave),
                        (byte)(baseColors[0].G * wave),
                        (byte)(baseColors[0].B * wave)
                    ));
                }
                break;

            case RgbEffectType.SpectrumCycle:
                // All LEDs cycle through spectrum
                var cycleHue = (frame * effect.Speed * 3) % 360;
                var cycleColor = HslToRgb(cycleHue, 1.0, 0.5);
                for (int i = 0; i < 75; i++)
                {
                    colors.Add(cycleColor);
                }
                break;

            default:
                // Default static
                for (int i = 0; i < 75; i++)
                {
                    colors.Add(baseColors[0]);
                }
                break;
        }

        return colors;
    }

    private static RgbColor HslToRgb(double hue, double saturation, double lightness)
    {
        double c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        double x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
        double m = lightness - c / 2;

        double r, g, b;

        if (hue < 60)
        {
            r = c; g = x; b = 0;
        }
        else if (hue < 120)
        {
            r = x; g = c; b = 0;
        }
        else if (hue < 180)
        {
            r = 0; g = c; b = x;
        }
        else if (hue < 240)
        {
            r = 0; g = x; b = c;
        }
        else if (hue < 300)
        {
            r = x; g = 0; b = c;
        }
        else
        {
            r = c; g = 0; b = x;
        }

        return new RgbColor(
            (byte)((r + m) * 255),
            (byte)((g + m) * 255),
            (byte)((b + m) * 255)
        );
    }
}
