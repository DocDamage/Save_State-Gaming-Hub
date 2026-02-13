using Avalonia.Controls;
using SaveState.Presentation.ViewModels.Shell.Mugen;

namespace SaveState.Presentation.Views.Mugen.Sections;

/// <summary>
/// Main view for the MUGEN Move Creation system.
/// Provides a tabbed interface for browsing templates, editing moves, validation, balancing, export, and testing.
/// </summary>
public partial class MoveCreationView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveCreationView"/> class.
    /// </summary>
    public MoveCreationView()
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