using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.DTOs;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

public partial class MugenTrainingViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _selectedCharacter;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _dummyCharacter;

    [ObservableProperty]
    private string _trainingStatus = "Ready to start session";

    public MugenTrainingViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "TRAINING MODE";
    }

    [RelayCommand]
    private async Task StartTrainingAsync()
    {
        if (SelectedCharacter == null) return;

        try
        {
            TrainingStatus = $"Launching training: {SelectedCharacter.Name}...";

            // In a real app, this would use IMugenLauncher.LaunchTrainingAsync
            await _mediator.Send(new Application.Mugen.Commands.LaunchIkemenVersusCommand(SelectedCharacter.Name, DummyCharacter?.Name ?? "KFM"));

            TrainingStatus = "Training session active.";
        }
        catch (Exception ex)
        {
            TrainingStatus = $"Launch failed: {ex.Message}";
        }
    }

}
