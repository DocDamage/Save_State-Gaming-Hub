using Avalonia.Controls;
using Avalonia.Input;

namespace SaveState.Presentation.Views.Shell;

/// <summary>
/// The title bar view for the application shell.
/// </summary>
public partial class TitleBarView : UserControl
{
    public TitleBarView()
    {
        InitializeComponent();
    }

    private void OnDragRegionPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // Allow dragging the window
            var window = (Window?)this.VisualRoot;
            window?.BeginMoveDrag(e);
        }
    }
}