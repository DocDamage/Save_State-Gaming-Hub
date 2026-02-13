using Avalonia.Controls;
using SaveState.Presentation.ViewModels.Shell.Mugen;

namespace SaveState.Presentation.Views.Mugen.Sections;

/// <summary>
/// View for browsing and selecting move templates.
/// </summary>
public partial class MoveTemplateBrowserView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveTemplateBrowserView"/> class.
    /// </summary>
    public MoveTemplateBrowserView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the view model for this view.
    /// </summary>
    public MoveCreationViewModel? ViewModel
    {
        get => DataContext as MoveCreationViewModel;
        set => DataContext = value;
    }
}