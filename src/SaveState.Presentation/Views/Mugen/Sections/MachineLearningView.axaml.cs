using Avalonia.Controls;
using SaveState.Presentation.ViewModels.Shell.Mugen;

namespace SaveState.Presentation.Views.Mugen.Sections;

/// <summary>
/// View for the MUGEN Machine Learning and Predictive Analytics system.
/// Provides interfaces for match prediction, character analysis, and procedural content generation.
/// </summary>
public partial class MachineLearningView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MachineLearningView"/> class.
    /// </summary>
    public MachineLearningView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the view model for this view.
    /// </summary>
    public MachineLearningViewModel? ViewModel
    {
        get => DataContext as MachineLearningViewModel;
        set => DataContext = value;
    }
}