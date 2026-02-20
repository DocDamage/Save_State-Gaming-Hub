using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Tools;

namespace SaveState.Presentation.Views.Tools;

/// <summary>
/// View for the signature testing tool.
/// </summary>
public partial class SignatureTesterView : UserControl
{
    public SignatureTesterView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
