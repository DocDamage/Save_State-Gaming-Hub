using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.Views.Overlays;

/// <summary>
/// The voice command help overlay view.
/// </summary>
public partial class VoiceCommandHelpOverlay : UserControl
{
    private readonly IOverlayService? _overlayService;

    /// <summary>
    /// Creates a new voice command help overlay view.
    /// </summary>
    public VoiceCommandHelpOverlay()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Creates a new voice command help overlay view with overlay service.
    /// </summary>
    public VoiceCommandHelpOverlay(IOverlayService overlayService) : this()
    {
        _overlayService = overlayService;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        // Hide this overlay
        if (_overlayService is not null)
        {
            // The help overlay is shown via AI assistant overlay in the current implementation
            _overlayService.HideAiAssistantOverlay();
        }
        else if (Parent is Control parentControl)
        {
            parentControl.IsVisible = false;
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            OnCloseClick(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }
}
