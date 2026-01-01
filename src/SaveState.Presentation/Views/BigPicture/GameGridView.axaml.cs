using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.BigPicture;

public partial class GameGridView : UserControl
{
    public GameGridView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Handle controller/d-pad navigation
        var viewModel = DataContext as ViewModels.BigPicture.GameGridViewModel;
        if (viewModel == null) return;

        switch (e.Key)
        {
            case Key.Up:
                viewModel.MoveSelection(-1, 0);
                e.Handled = true;
                break;
            case Key.Down:
                viewModel.MoveSelection(1, 0);
                e.Handled = true;
                break;
            case Key.Left:
                viewModel.MoveSelection(0, -1);
                e.Handled = true;
                break;
            case Key.Right:
                viewModel.MoveSelection(0, 1);
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Space:
                viewModel.LaunchSelectedGame();
                e.Handled = true;
                break;
        }
    }
}