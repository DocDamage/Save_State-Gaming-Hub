using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.MobileCompanion;

namespace SaveState.Presentation.Views.MobileCompanion;

/// <summary>
/// Mobile notifications view for the companion app.
/// </summary>
public partial class MobileNotificationsView : UserControl
{
    public MobileNotificationsView()
    {
        InitializeComponent();
    }

    public MobileNotificationsView(MobileNotificationsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
