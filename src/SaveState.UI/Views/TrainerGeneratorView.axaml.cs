using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.UI.Views;

public partial class TrainerGeneratorView : UserControl
{
    public TrainerGeneratorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
