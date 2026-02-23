using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SaveState.Presentation.Controls.Loading;

/// <summary>
/// A customizable loading spinner control with optional message display.
/// 
/// Usage:
/// <code>
/// &lt;local:LoadingSpinner Size="Large" Message="Loading game library..." /&gt;
/// </code>
/// </summary>
public partial class LoadingSpinner : UserControl
{
    /// <summary>
    /// Defines the <see cref="Size"/> property.
    /// </summary>
    public static readonly StyledProperty<SpinnerSize> SizeProperty =
        AvaloniaProperty.Register<LoadingSpinner, SpinnerSize>(
            nameof(Size),
            SpinnerSize.Normal);

    /// <summary>
    /// Defines the <see cref="Message"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<LoadingSpinner, string?>(
            nameof(Message));

    /// <summary>
    /// Defines the <see cref="SpinnerColor"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush> SpinnerColorProperty =
        AvaloniaProperty.Register<LoadingSpinner, IBrush>(
            nameof(SpinnerColor),
            new SolidColorBrush(Color.Parse("#0078D4")));

    /// <summary>
    /// Defines the <see cref="TrackColor"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush> TrackColorProperty =
        AvaloniaProperty.Register<LoadingSpinner, IBrush>(
            nameof(TrackColor),
            new SolidColorBrush(Colors.LightGray));

    /// <summary>
    /// Defines the <see cref="ShowMessage"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowMessageProperty =
        AvaloniaProperty.Register<LoadingSpinner, bool>(
            nameof(ShowMessage),
            false);

    /// <summary>
    /// Gets or sets the size of the spinner.
    /// </summary>
    public SpinnerSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the loading message text.
    /// </summary>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Gets or sets the color of the spinner arc.
    /// </summary>
    public IBrush SpinnerColor
    {
        get => GetValue(SpinnerColorProperty);
        set => SetValue(SpinnerColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the color of the background track.
    /// </summary>
    public IBrush TrackColor
    {
        get => GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the loading message.
    /// </summary>
    public bool ShowMessage
    {
        get => GetValue(ShowMessageProperty);
        set => SetValue(ShowMessageProperty, value);
    }

    public LoadingSpinner()
    {
        InitializeComponent();
        UpdatePseudoClasses();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SizeProperty)
        {
            UpdatePseudoClasses();
        }
        else if (change.Property == MessageProperty)
        {
            ShowMessage = !string.IsNullOrEmpty(Message);
        }
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":small", Size == SpinnerSize.Small);
        PseudoClasses.Set(":normal", Size == SpinnerSize.Normal);
        PseudoClasses.Set(":large", Size == SpinnerSize.Large);
    }
}

/// <summary>
/// Defines the available sizes for the loading spinner.
/// </summary>
public enum SpinnerSize
{
    /// <summary>
    /// Small spinner (16x16 pixels).
    /// </summary>
    Small,

    /// <summary>
    /// Normal spinner (32x32 pixels).
    /// </summary>
    Normal,

    /// <summary>
    /// Large spinner (64x64 pixels).
    /// </summary>
    Large
}
