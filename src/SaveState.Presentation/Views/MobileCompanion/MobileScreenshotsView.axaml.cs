using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.MobileCompanion;

namespace SaveState.Presentation.Views.MobileCompanion;

/// <summary>
/// Mobile screenshots view for the companion app.
/// </summary>
public partial class MobileScreenshotsView : UserControl
{
    public MobileScreenshotsView()
    {
        InitializeComponent();
    }

    public MobileScreenshotsView(MobileScreenshotsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
