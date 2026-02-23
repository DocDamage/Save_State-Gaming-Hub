using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.MobileCompanion;

namespace SaveState.Presentation.Views.MobileCompanion;

/// <summary>
/// Mobile dashboard view for the companion app.
/// </summary>
public partial class MobileDashboardView : UserControl
{
    public MobileDashboardView()
    {
        InitializeComponent();
    }

    public MobileDashboardView(MobileDashboardViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
