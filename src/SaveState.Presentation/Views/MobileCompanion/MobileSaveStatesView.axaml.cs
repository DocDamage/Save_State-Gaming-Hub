using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.MobileCompanion;

namespace SaveState.Presentation.Views.MobileCompanion;

/// <summary>
/// Mobile save states view for the companion app.
/// </summary>
public partial class MobileSaveStatesView : UserControl
{
    public MobileSaveStatesView()
    {
        InitializeComponent();
    }

    public MobileSaveStatesView(MobileSaveStatesViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
