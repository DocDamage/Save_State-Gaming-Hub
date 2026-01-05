using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Analytics;

namespace SaveState.Presentation.Views.Analytics;

public partial class AnalyticsDashboardView : UserControl
{
    public AnalyticsDashboardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
