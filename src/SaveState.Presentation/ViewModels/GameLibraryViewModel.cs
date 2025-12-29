namespace SaveState.Presentation.ViewModels;

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.GameLibrary.Entities;

public partial class GameLibraryViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    public GameLibraryViewModel(IMediator mediator)
    {
        _mediator = mediator;
        LoadGamesCommand = new AsyncRelayCommand(LoadGamesAsync);
        Games = new ObservableCollection<Game>();

        // Auto-load games when ViewModel is created (for Walking Skeleton)
        _ = LoadGamesAsync();
    }

    public ObservableCollection<Game> Games { get; }

    public IAsyncRelayCommand LoadGamesCommand { get; }

    private async Task LoadGamesAsync()
    {
        var query = new GetAllGamesQuery();
        var games = await _mediator.Send(query);

        Games.Clear();
        foreach (var game in games)
        {
            Games.Add(game);
        }
    }
}
