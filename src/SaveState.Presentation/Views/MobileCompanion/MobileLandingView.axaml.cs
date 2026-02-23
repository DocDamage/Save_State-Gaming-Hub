using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.MobileCompanion;

namespace SaveState.Presentation.Views.MobileCompanion;

/// <summary>
/// Mobile landing view for companion app pairing.
/// </summary>
public partial class MobileLandingView : UserControl
{
    public MobileLandingView()
    {
        InitializeComponent();
    }

    public MobileLandingView(MobileLandingViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
