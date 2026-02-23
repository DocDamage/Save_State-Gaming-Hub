using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.RgbSync;

namespace SaveState.Presentation.Views.RgbSync;

public partial class RgbProfileManagerView : UserControl
{
    public RgbProfileManagerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
