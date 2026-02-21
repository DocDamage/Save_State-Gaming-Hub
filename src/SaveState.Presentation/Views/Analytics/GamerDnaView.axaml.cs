using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Analytics;

/// <summary>
/// View for displaying the Gaming DNA Profile.
/// </summary>
public partial class GamerDnaView : UserControl
{
    public GamerDnaView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
