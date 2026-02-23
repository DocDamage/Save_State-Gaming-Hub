using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Dialog view for displaying detailed game performance statistics.
/// Shows FPS history, hardware utilization, and optimization suggestions.
/// </summary>
public partial class GamePerformanceDetailView : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GamePerformanceDetailView"/> class.
    /// </summary>
    public GamePerformanceDetailView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
