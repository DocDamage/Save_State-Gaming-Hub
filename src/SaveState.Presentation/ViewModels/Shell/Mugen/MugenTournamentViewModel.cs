using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.DTOs;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

public partial class MugenTournamentViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;

    public ObservableCollection<MugenCharacterSummaryDto> Participants { get; } = new();

    [ObservableProperty]
    private string _tournamentName = "New Tournament";

    [ObservableProperty]
    private bool _isTournamentActive;

    public MugenTournamentViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "TOURNAMENT MODE";
    }

    [RelayCommand]
    private void CreateTournament()
    {
        IsTournamentActive = true;
    }

    [RelayCommand]
    private void RefreshTournament()
    {
        // Placeholder refresh logic
        // In a real implementation, this would reload bracket data from the service
    }
}
