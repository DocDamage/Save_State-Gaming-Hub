using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace SaveState.Presentation.Controls.ColorPicker;

/// <summary>
/// A color picker control with color wheel, RGB sliders, and HEX input.
/// </summary>
public partial class ThemeColorPicker : UserControl
{
    #region Dependency Properties

    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<ThemeColorPicker, Color>(nameof(Color), defaultValue: Colors.Purple, coerce: CoerceColor);

    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<ThemeColorPicker, string>(nameof(Header), defaultValue: "Color");

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ThemeColorPicker, bool>(nameof(IsOpen), defaultValue: false);

    public static readonly StyledProperty<byte> RedValueProperty =
        AvaloniaProperty.Register<ThemeColorPicker, byte>(nameof(RedValue), defaultValue: 103);

    public static readonly StyledProperty<byte> GreenValueProperty =
        AvaloniaProperty.Register<ThemeColorPicker, byte>(nameof(GreenValue), defaultValue: 80);

    public static readonly StyledProperty<byte> BlueValueProperty =
        AvaloniaProperty.Register<ThemeColorPicker, byte>(nameof(BlueValue), defaultValue: 164);

    #endregion

    #region Properties

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public byte RedValue
    {
        get => GetValue(RedValueProperty);
        set => SetValue(RedValueProperty, value);
    }

    public byte GreenValue
    {
        get => GetValue(GreenValueProperty);
        set => SetValue(GreenValueProperty, value);
    }

    public byte BlueValue
    {
        get => GetValue(BlueValueProperty);
        set => SetValue(BlueValueProperty, value);
    }

    public string HexColor => $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";

    public IBrush SelectedColorBrush => new SolidColorBrush(Color);

    public IBrush ColorWheelBrush => CreateColorWheelBrush();

    public IBrush ContrastColor => GetContrastColor(Color);

    #endregion

    private bool _isDragging;
    private WriteableBitmap? _colorWheelBitmap;

    public ThemeColorPicker()
    {
        InitializeComponent();
        InitializeColorWheel();
        UpdateFromColor(Color);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ColorProperty)
        {
            UpdateFromColor(Color);
        }
        else if (change.Property == RedValueProperty ||
                 change.Property == GreenValueProperty ||
                 change.Property == BlueValueProperty)
        {
            UpdateFromRgb();
        }
    }

    private static Color CoerceColor(AvaloniaObject obj, Color color)
    {
        return color;
    }

    private void InitializeColorWheel()
    {
        const int size = 200;
        _colorWheelBitmap = new WriteableBitmap(
            new PixelSize(size, size),
            new Vector(96, 96),
            PixelFormat.Rgba8888,
            AlphaFormat.Opaque);

        RenderColorWheel();
    }

    private void RenderColorWheel()
    {
        if (_colorWheelBitmap == null) return;

        var size = _colorWheelBitmap.PixelSize.Width;
        var centerX = size / 2;
        var centerY = size / 2;
        var radius = size / 2 - 5;

        using var buffer = _colorWheelBitmap.Lock();
        var stride = buffer.RowBytes;
        var pixelData = new byte[size * stride];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance <= radius)
                {
                    var angle = Math.Atan2(dy, dx) * 180 / Math.PI;
                    if (angle < 0) angle += 360;

                    var saturation = distance / radius;
                    var (r, g, b) = HsvToRgb(angle, saturation, 1.0);

                    var index = y * stride + x * 4;
                    if (index + 3 < pixelData.Length)
                    {
                        pixelData[index] = (byte)(r * 255);
                        pixelData[index + 1] = (byte)(g * 255);
                        pixelData[index + 2] = (byte)(b * 255);
                        pixelData[index + 3] = 255;
                    }
                }
                else
                {
                    var index = y * stride + x * 4;
                    if (index + 3 < pixelData.Length)
                    {
                        pixelData[index] = 240;
                        pixelData[index + 1] = 240;
                        pixelData[index + 2] = 240;
                        pixelData[index + 3] = 255;
                    }
                }
            }
        }

        System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, buffer.Address, pixelData.Length);
    }

    private IBrush CreateColorWheelBrush()
    {
        if (_colorWheelBitmap == null)
        {
            InitializeColorWheel();
        }
        return new ImageBrush(_colorWheelBitmap) { Stretch = Stretch.Fill };
    }

    private void UpdateFromColor(Color color)
    {
        RedValue = color.R;
        GreenValue = color.G;
        BlueValue = color.B;
    }

    private void UpdateFromRgb()
    {
        Color = Color.FromRgb(RedValue, GreenValue, BlueValue);
    }

    private static IBrush GetContrastColor(Color background)
    {
        // Calculate luminance
        var r = background.R / 255.0;
        var g = background.G / 255.0;
        var b = background.B / 255.0;

        var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
        return luminance > 0.5 ? Brushes.Black : Brushes.White;
    }

    private static (double r, double g, double b) HsvToRgb(double h, double s, double v)
    {
        var c = v * s;
        var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        var m = v - c;

        double r, g, b;

        if (h < 60)
        {
            r = c; g = x; b = 0;
        }
        else if (h < 120)
        {
            r = x; g = c; b = 0;
        }
        else if (h < 180)
        {
            r = 0; g = c; b = x;
        }
        else if (h < 240)
        {
            r = 0; g = x; b = c;
        }
        else if (h < 300)
        {
            r = x; g = 0; b = c;
        }
        else
        {
            r = c; g = 0; b = x;
        }

        return (r + m, g + m, b + m);
    }

    private void OnColorWheelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDragging = true;
        UpdateColorFromPosition(e.GetPosition(ColorWheelCanvas));
        e.Pointer.Capture(ColorWheelCanvas);
    }

    private void OnColorWheelPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            UpdateColorFromPosition(e.GetPosition(ColorWheelCanvas));
        }
    }

    private void OnColorWheelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        e.Pointer.Capture(null);
    }

    private void UpdateColorFromPosition(Point position)
    {
        if (ColorWheelCanvas == null) return;

        var centerX = ColorWheelCanvas.Bounds.Width / 2;
        var centerY = ColorWheelCanvas.Bounds.Height / 2;
        var dx = position.X - centerX;
        var dy = position.Y - centerY;
        var distance = Math.Min(Math.Sqrt(dx * dx + dy * dy), centerX - 5);
        var angle = Math.Atan2(dy, dx) * 180 / Math.PI;
        if (angle < 0) angle += 360;

        var saturation = distance / (centerX - 5);
        var (r, g, b) = HsvToRgb(angle, saturation, 1.0);

        Color = Color.FromRgb(
            (byte)(r * 255),
            (byte)(g * 255),
            (byte)(b * 255));

        // Update selector position
        if (ColorSelector != null)
        {
            var selectorX = centerX + distance * Math.Cos(angle * Math.PI / 180) - 8;
            var selectorY = centerY + distance * Math.Sin(angle * Math.PI / 180) - 8;

            Canvas.SetLeft(ColorSelector, selectorX);
            Canvas.SetTop(ColorSelector, selectorY);
            ColorSelector.IsVisible = true;
        }
    }

    private void OnOpenClick(object? sender, PointerPressedEventArgs e)
    {
        IsOpen = true;
    }

    private void OnCloseClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsOpen = false;
    }
}
