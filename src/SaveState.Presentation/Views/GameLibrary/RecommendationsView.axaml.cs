using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.GameLibrary;

/// <summary>
/// View for Smart Recommendations 2.0 with hybrid recommendation engine.
/// </summary>
public partial class RecommendationsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationsView"/> class.
    /// </summary>
    public RecommendationsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
