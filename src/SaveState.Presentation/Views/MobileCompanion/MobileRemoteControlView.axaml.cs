using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.MobileCompanion;

namespace SaveState.Presentation.Views.MobileCompanion;

/// <summary>
/// Mobile remote control view for the companion app.
/// </summary>
public partial class MobileRemoteControlView : UserControl
{
    public MobileRemoteControlView()
    {
        InitializeComponent();
    }

    public MobileRemoteControlView(MobileRemoteControlViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
