using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.RgbSync;

namespace SaveState.Presentation.Views.RgbSync;

public partial class RgbColorPicker : Window
{
    public RgbColorPicker()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
