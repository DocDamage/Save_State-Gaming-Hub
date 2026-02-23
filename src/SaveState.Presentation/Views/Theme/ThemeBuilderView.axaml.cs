using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Theme;

namespace SaveState.Presentation.Views.Theme;

/// <summary>
/// View for the Theme Builder feature.
/// </summary>
public partial class ThemeBuilderView : UserControl
{
    public ThemeBuilderView()
    {
        InitializeComponent();
    }

    public ThemeBuilderView(ThemeBuilderViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
