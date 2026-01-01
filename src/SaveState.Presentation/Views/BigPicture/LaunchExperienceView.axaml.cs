using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.BigPicture;

public partial class LaunchExperienceView : UserControl
{
    public LaunchExperienceView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}