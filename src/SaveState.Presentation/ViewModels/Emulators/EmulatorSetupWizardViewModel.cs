using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.RomManagement.Services;
using SaveState.Core.Common;

namespace SaveState.Presentation.ViewModels.Emulators;

public partial class EmulatorSetupWizardViewModel : ObservableObject
{
    private readonly IEmulatorInstallationService _installationService;

    [ObservableProperty]
    private int _currentStep;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private double _totalProgress;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public Action? RequestClose { get; set; }

    public ObservableCollection<EmulatorOptionViewModel> AvailableEmulators { get; } = new();

    public EmulatorSetupWizardViewModel(IEmulatorInstallationService installationService)
    {
        _installationService = installationService;
        StatusMessage = "Fetching available emulators...";
        _ = LoadOptionsAsync();
    }

    private async Task LoadOptionsAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _installationService.GetAvailableOptionsAsync();
            if (result.IsSuccess && result.Value is not null)
            {
                AvailableEmulators.Clear();
                foreach (var option in result.Value)
                {
                    AvailableEmulators.Add(new EmulatorOptionViewModel(option));
                }
                StatusMessage = "Select the emulators you want to install.";
            }
            else
            {
                StatusMessage = $"Error: {result.Error}";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task StartInstallationAsync()
    {
        var selected = AvailableEmulators.Where(e => e.IsSelected).ToList();
        if (!selected.Any())
        {
            StatusMessage = "Please select at least one emulator.";
            return;
        }

        CurrentStep = 1; // Progress Step
        IsBusy = true;
        TotalProgress = 0;

        try
        {
            foreach (var emulator in selected)
            {
                StatusMessage = $"Installing {emulator.Option.Name}...";
                var progress = new Progress<double>(p =>
                {
                    // For multiple emulators, we would ideally weight them,
                    // but for now we just show current one's progress or average.
                    TotalProgress = p;
                });

                var result = await _installationService.DownloadAndInstallAsync(emulator.Option, progress);
                if (!result.IsSuccess)
                {
                    StatusMessage = $"Failed to install {emulator.Option.Name}: {result.Error}";
                    return;
                }
            }

            CurrentStep = 2; // Finished Step
            StatusMessage = "Installation completed successfully!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NextStep() => CurrentStep++;

    [RelayCommand]
    private void PreviousStep() => CurrentStep--;

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    [RelayCommand]
    private void Finish() => RequestClose?.Invoke();
}

public partial class EmulatorOptionViewModel : ObservableObject
{
    public EmulatorInstallationOption Option { get; }

    [ObservableProperty]
    private bool _isSelected;

    public EmulatorOptionViewModel(EmulatorInstallationOption option)
    {
        Option = option;
    }
}
