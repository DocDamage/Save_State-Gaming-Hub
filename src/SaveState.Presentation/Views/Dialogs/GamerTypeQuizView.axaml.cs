using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Dialog view for the Gamer Type Quiz.
/// </summary>
public partial class GamerTypeQuizView : UserControl
{
    public GamerTypeQuizView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
