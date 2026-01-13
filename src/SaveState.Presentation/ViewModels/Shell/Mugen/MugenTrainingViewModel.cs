using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

public partial class MugenTrainingViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IMugenTrainingService _trainingService;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _selectedCharacter;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _dummyCharacter;

    [ObservableProperty]
    private string _trainingStatus = "Ready to start session";

    [ObservableProperty]
    private bool _isSessionActive;

    public MugenTrainingViewModel(IMediator mediator, IMugenTrainingService trainingService)
    {
        _mediator = mediator;
        _trainingService = trainingService;
        Title = "TRAINING MODE";
    }

    [RelayCommand]
    private async Task StartTrainingAsync()
    {
        if (SelectedCharacter == null)
        {
            TrainingStatus = "Please select a character first.";
            return;
        }

        try
        {
            TrainingStatus = $"Launching training: {SelectedCharacter.Name}...";

            var config = new TrainingConfig(
                DummyCharacter?.Id ?? Guid.Empty,
                DummyBehavior.Block,
                true,
                true,
                true
            );

            var result = await _trainingService.StartSessionAsync(SelectedCharacter.Id, config);

            if (result.IsSuccess)
            {
                IsSessionActive = true;
                TrainingStatus = "Training session active.";
                // In a real app, this would also trigger the actual process launch via the service backing this
            }
            else
            {
               TrainingStatus = $"Launch failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            TrainingStatus = $"Launch failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task EndTrainingAsync()
    {
         // Logic to end session
         IsSessionActive = false;
         TrainingStatus = "Session ended.";
         await Task.CompletedTask;
    }

}
