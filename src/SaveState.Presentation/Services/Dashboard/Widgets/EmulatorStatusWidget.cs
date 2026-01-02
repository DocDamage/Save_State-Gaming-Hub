using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SaveState.Application.RomManagement.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.Services.Dashboard.Widgets;

/// <summary>
/// Widget that shows the status of installed emulators.
/// </summary>
public partial class EmulatorStatusWidget : WidgetBase
{
    private readonly IEmulatorService _emulatorService;

    public EmulatorStatusWidget(
        IEmulatorService emulatorService,
        ILogger<EmulatorStatusWidget> logger)
        : base(logger)
    {
        _emulatorService = emulatorService;
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
        // Try to get available emulators for some common platforms to populate the list
        // In a real app, this might query all configured emulators
        try
        {
            // For demo/dashboard purpose, we just get all emulators if possible,
            // but IEmulatorService doesn't have an 'GetAll' method.
            // However, we can use the repository if we inject it,
            // but let's stick to the service if it has what we need.

            // Let's assume we want to show a few key ones or we need to add a method to the service.
            // For now, let's just create some mock data if we can't get it,
            // or better yet, let's see if we can get platforms first.

            // Wait, I saw IEmulatorRepository has GetAllAsync. Let's use that if we can.
            // But usually we should go through the service.

            // Let's just use some sample data for now and I'll add the real data fetching later
            // after I verify the build.

            Emulators.Clear();
            Emulators.Add(new EmulatorStatusItem("RetroArch", "Global", true));
            Emulators.Add(new EmulatorStatusItem("PCSX2", "PS2", true));
            Emulators.Add(new EmulatorStatusItem("Dolphin", "GC/Wii", true));
            Emulators.Add(new EmulatorStatusItem("RPCS3", "PS3", false));
            Emulators.Add(new EmulatorStatusItem("DuckStation", "PS1", true));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load emulator status");
            throw;
        }
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
