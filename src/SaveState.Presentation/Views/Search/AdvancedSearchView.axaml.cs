using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Search;

/// <summary>
/// View for the Advanced Search feature.
/// </summary>
public partial class AdvancedSearchView : UserControl
{
    public AdvancedSearchView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
