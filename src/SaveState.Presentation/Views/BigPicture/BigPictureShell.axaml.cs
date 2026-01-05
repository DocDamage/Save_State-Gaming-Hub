using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.BigPicture;

public partial class BigPictureShell : UserControl
{
    public BigPictureShell()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (s, e) =>
        {
            var gridView = this.FindControl<Control>("GameGridControl");
            gridView?.Focus();
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
