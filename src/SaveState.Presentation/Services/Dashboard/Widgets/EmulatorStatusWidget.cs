using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Application.RomManagement.Services;
using SaveState.Core.RomManagement;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.Services.Dashboard.Widgets;

/// <summary>
/// Widget that shows the status of installed emulators.
/// </summary>
public partial class EmulatorStatusWidget : WidgetBase
{
    private readonly IEmulatorService _emulatorService;
    private readonly IEmulatorRepository _emulatorRepository;
    private readonly INavigationService _navigationService;

    public EmulatorStatusWidget(
        IEmulatorService emulatorService,
        IEmulatorRepository emulatorRepository,
        INavigationService navigationService,
        ILogger<EmulatorStatusWidget> logger)
        : base(logger)
    {
        _emulatorService = emulatorService;
        _emulatorRepository = emulatorRepository;
        _navigationService = navigationService;
        Emulators = new ObservableCollection<EmulatorStatusItem>();
    }

    /// <inheritdoc />
    public override string Id => "emulator-status";

    /// <inheritdoc />
    public override string Title => "Emulators";

    /// <inheritdoc />
    public override string Icon => "🎮";

    /// <inheritdoc />
    public override WidgetSize DefaultSize => WidgetSize.Medium;

    /// <inheritdoc />
    public override WidgetSize[] SupportedSizes => new[] { WidgetSize.Medium, WidgetSize.Large };

    /// <summary>
    /// Gets the collection of emulator status items.
    /// </summary>
    public ObservableCollection<EmulatorStatusItem> Emulators { get; }

    /// <inheritdoc />
    protected override async Task LoadDataAsync()
    {
        try
        {
            Emulators.Clear();

            // Get all configured emulators from the repository
            var emulators = await _emulatorRepository.GetAllAsync();

            foreach (var emulator in emulators.OrderBy(e => e.Name))
            {
                // Check if emulator executable exists
                var isAvailable = emulator.IsAvailable;

                Emulators.Add(new EmulatorStatusItem(
                    emulator.Name,
                    emulator.Platform?.Name.Value ?? "Unknown",
                    isAvailable));
            }

            // If no emulators are configured, show a helpful message
            if (Emulators.Count == 0)
            {
                Emulators.Add(new EmulatorStatusItem(
                    "No emulators configured",
                    "Add emulators in Settings",
                    false));
            }

            Logger.LogInformation("Loaded {Count} emulator status items", Emulators.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load emulator status");

            // Fallback data
            Emulators.Clear();
            Emulators.Add(new EmulatorStatusItem(
                "Error loading emulators",
                "Check logs",
                false));
        }
    }

    [RelayCommand]
    private async Task NavigateToSettings()
    {
        await _navigationService.NavigateTo("Settings");
    }
}

/// <summary>
/// Represents the status of a single emulator.
/// </summary>
public class EmulatorStatusItem : ObservableObject
{
    public EmulatorStatusItem(string name, string platform, bool isAvailable)
    {
        Name = name;
        Platform = platform;
        IsAvailable = isAvailable;
    }

    public string Name { get; }
    public string Platform { get; }
    public bool IsAvailable { get; }
    public string StatusIcon => IsAvailable ? "🟢" : "🔴";
    public string StatusText => IsAvailable ? "Ready" : "Missing";
}
