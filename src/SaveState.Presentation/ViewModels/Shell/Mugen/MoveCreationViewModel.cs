using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Presentation.ViewModels.Shell.Mugen;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

/// <summary>
/// View model for professional move creation engine in MUGEN.
/// Allows users to create, edit, and test custom moves for characters.
/// </summary>
public partial class MoveCreationViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IMoveCreationService _moveCreationService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private string _moveName = string.Empty;

    [ObservableProperty]
    private string _moveCommand = string.Empty;

    [ObservableProperty]
    private string _moveType = "Normal";

    [ObservableProperty]
    private int _moveDamage = 100;

    [ObservableProperty]
    private int _startup = 5;

    [ObservableProperty]
    private int _active = 3;

    [ObservableProperty]
    private int _recovery = 10;

    [ObservableProperty]
    private int _blockAdvantage = -2;

    [ObservableProperty]
    private int _hitAdvantage = 5;

    [ObservableProperty]
    private string _properties = "None";

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _animationFrames = string.Empty;

    [ObservableProperty]
    private string _hitboxData = string.Empty;

    [ObservableProperty]
    private string _soundEffects = string.Empty;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _selectedCharacter;

    [ObservableProperty]
    private MugenMoveEntry? _selectedMove;

    public ObservableCollection<MugenCharacterSummaryDto> AvailableCharacters { get; } = new();
    public ObservableCollection<MugenMoveEntry> ExistingMoves { get; } = new();
    public ObservableCollection<string> MoveTypes { get; } = new() 
    { 
        "Normal", "Special", "Super", "Hyper", "Throw", "Counter", "Projectile" 
    };
    public ObservableCollection<string> PropertyOptions { get; } = new()
    {
        "None", "Invincible", "SuperArmor", "Projectile", "Unblockable", "Guard Break"
    };

    public MoveCreationViewModel(
        IMediator mediator,
        IMoveCreationService moveCreationService,
        INotificationService notificationService)
    {
        _mediator = mediator;
        _moveCreationService = moveCreationService;
        _notificationService = notificationService;
        Title = "Professional Move Creation Engine";
    }

    public override async Task InitializeAsync()
    {
        await LoadCharactersAsync();
        await base.InitializeAsync();
    }

    [RelayCommand]
    private async Task LoadCharactersAsync()
    {
        try
        {
            var result = await _moveCreationService.GetAvailableCharactersAsync();
            if (result.IsSuccess && result.Value != null)
            {
                AvailableCharacters.Clear();
                foreach (var character in result.Value)
                {
                    AvailableCharacters.Add(character);
                }
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Failed to load characters: {ex.Message}";
        }
    }

    partial void OnSelectedCharacterChanged(MugenCharacterSummaryDto? value)
    {
        if (value != null)
        {
            _ = LoadExistingMovesAsync();
        }
    }

    [RelayCommand]
    private async Task LoadExistingMovesAsync()
    {
        if (SelectedCharacter == null) return;

        try
        {
            var result = await _moveCreationService.GetCharacterMovesAsync(SelectedCharacter.Id);
            if (result.IsSuccess && result.Value != null)
            {
                ExistingMoves.Clear();
                foreach (var move in result.Value)
                {
                    ExistingMoves.Add(move);
                }
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Failed to load moves: {ex.Message}";
        }
    }

    partial void OnSelectedMoveChanged(MugenMoveEntry? value)
    {
        if (value != null)
        {
            MoveName = value.MoveName;
            MoveCommand = value.Command;
            MoveType = value.Type;
            MoveDamage = value.Damage;
            Startup = value.Startup;
            Active = value.Active;
            Recovery = value.Recovery;
            BlockAdvantage = value.BlockAdvantage;
            HitAdvantage = value.HitAdvantage;
            Properties = value.Properties;
            Notes = value.Notes ?? string.Empty;
        }
    }

    [RelayCommand]
    private async Task CreateMoveAsync()
    {
        if (SelectedCharacter == null)
        {
            ValidationMessage = "Please select a character first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(MoveName))
        {
            ValidationMessage = "Move name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(MoveCommand))
        {
            ValidationMessage = "Move command is required.";
            return;
        }

        IsSaving = true;
        ValidationMessage = "Creating move...";

        try
        {
            var moveData = new MoveData(
                MoveName,
                MoveCommand,
                MoveType,
                MoveDamage,
                Startup,
                Active,
                Recovery,
                BlockAdvantage,
                HitAdvantage,
                Properties,
                AnimationFrames,
                HitboxData,
                SoundEffects,
                Notes
            );

            var result = await _moveCreationService.CreateMoveAsync(SelectedCharacter.Id, moveData);
            
            if (result.IsSuccess)
            {
                ValidationMessage = $"Move '{MoveName}' created successfully!";
                _notificationService.ShowSuccess($"Move '{MoveName}' created successfully!");
                await LoadExistingMovesAsync();
                ClearFormCommand.Execute(null);
            }
            else
            {
                ValidationMessage = $"Failed to create move: {result.Error}";
                _notificationService.ShowError(result.Error ?? "Failed to create move");
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Error creating move: {ex.Message}";
            _notificationService.ShowError($"Error: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task UpdateMoveAsync()
    {
        if (SelectedCharacter == null || SelectedMove == null)
        {
            ValidationMessage = "Please select a character and move first.";
            return;
        }

        IsSaving = true;
        ValidationMessage = "Updating move...";

        try
        {
            var moveData = new MoveData(
                MoveName,
                MoveCommand,
                MoveType,
                MoveDamage,
                Startup,
                Active,
                Recovery,
                BlockAdvantage,
                HitAdvantage,
                Properties,
                AnimationFrames,
                HitboxData,
                SoundEffects,
                Notes
            );

            var result = await _moveCreationService.UpdateMoveAsync(SelectedCharacter.Id, SelectedMove.MoveName, moveData);
            
            if (result.IsSuccess)
            {
                ValidationMessage = $"Move '{MoveName}' updated successfully!";
                _notificationService.ShowSuccess($"Move '{MoveName}' updated successfully!");
                await LoadExistingMovesAsync();
            }
            else
            {
                ValidationMessage = $"Failed to update move: {result.Error}";
                _notificationService.ShowError(result.Error ?? "Failed to update move");
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Error updating move: {ex.Message}";
            _notificationService.ShowError($"Error: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task DeleteMoveAsync()
    {
        if (SelectedCharacter == null || SelectedMove == null)
        {
            ValidationMessage = "Please select a character and move first.";
            return;
        }

        ValidationMessage = "Deleting move...";

        try
        {
            var result = await _moveCreationService.DeleteMoveAsync(SelectedCharacter.Id, SelectedMove.MoveName);
            
            if (result.IsSuccess)
            {
                ValidationMessage = $"Move '{SelectedMove.MoveName}' deleted successfully!";
                _notificationService.ShowSuccess($"Move '{SelectedMove.MoveName}' deleted successfully!");
                await LoadExistingMovesAsync();
                ClearFormCommand.Execute(null);
            }
            else
            {
                ValidationMessage = $"Failed to delete move: {result.Error}";
                _notificationService.ShowError(result.Error ?? "Failed to delete move");
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Error deleting move: {ex.Message}";
            _notificationService.ShowError($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TestMoveAsync()
    {
        if (SelectedCharacter == null)
        {
            ValidationMessage = "Please select a character first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(MoveName))
        {
            ValidationMessage = "Move name is required for testing.";
            return;
        }

        IsTesting = true;
        ValidationMessage = "Testing move...";

        try
        {
            var moveData = new MoveData(
                MoveName,
                MoveCommand,
                MoveType,
                MoveDamage,
                Startup,
                Active,
                Recovery,
                BlockAdvantage,
                HitAdvantage,
                Properties,
                AnimationFrames,
                HitboxData,
                SoundEffects,
                Notes
            );

            var result = await _moveCreationService.TestMoveAsync(SelectedCharacter.Id, moveData);
            
            if (result.IsSuccess)
            {
                ValidationMessage = result.Value ?? "Move test completed successfully!";
                _notificationService.ShowSuccess("Move test completed!");
            }
            else
            {
                ValidationMessage = $"Test failed: {result.Error}";
                _notificationService.ShowWarning(result.Error ?? "Test failed");
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Error testing move: {ex.Message}";
            _notificationService.ShowError($"Error: {ex.Message}");
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private void ClearForm()
    {
        MoveName = string.Empty;
        MoveCommand = string.Empty;
        MoveType = "Normal";
        MoveDamage = 100;
        Startup = 5;
        Active = 3;
        Recovery = 10;
        BlockAdvantage = -2;
        HitAdvantage = 5;
        Properties = "None";
        Notes = string.Empty;
        AnimationFrames = string.Empty;
        HitboxData = string.Empty;
        SoundEffects = string.Empty;
        SelectedMove = null;
        ValidationMessage = string.Empty;
    }

    [RelayCommand]
    private async Task ExportMoveAsync()
    {
        if (SelectedMove == null)
        {
            ValidationMessage = "Please select a move to export.";
            return;
        }

        try
        {
            var result = await _moveCreationService.ExportMoveAsync(SelectedCharacter!.Id, SelectedMove.MoveName);
            
            if (result.IsSuccess)
            {
                ValidationMessage = $"Move exported to: {result.Value}";
                _notificationService.ShowSuccess($"Move exported successfully!");
            }
            else
            {
                ValidationMessage = $"Export failed: {result.Error}";
                _notificationService.ShowError(result.Error ?? "Export failed");
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Error exporting move: {ex.Message}";
            _notificationService.ShowError($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportMoveAsync()
    {
        if (SelectedCharacter == null)
        {
            ValidationMessage = "Please select a character first.";
            return;
        }

        try
        {
            // In a real implementation, this would open a file dialog
            // For now, we'll show a placeholder message
            ValidationMessage = "Import feature: Select a .cmd or .cns file to import move data.";
            _notificationService.ShowInfo("Import functionality requires file selection dialog");
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Error importing move: {ex.Message}";
            _notificationService.ShowError($"Error: {ex.Message}");
        }
    }
}
