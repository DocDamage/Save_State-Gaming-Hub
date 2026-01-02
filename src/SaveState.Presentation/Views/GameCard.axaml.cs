namespace SaveState.Presentation.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using SaveState.Presentation.ViewModels;

public partial class GameCard : UserControl
{
    public GameCard()
    {
        InitializeComponent();
    }

    private void OnGameCardClick(object? sender, RoutedEventArgs e)
    {
        // Find the GameLibraryView parent
        var gameLibraryView = this.FindAncestorOfType<GameLibraryView>();
        if (gameLibraryView?.DataContext is GameLibraryViewModel viewModel && DataContext is GameSummaryViewModel game)
        {
            viewModel.OpenGameDetailCommand.Execute(game);
        }
    }
}
