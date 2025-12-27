using System.Text.Json;
using SaveState.Core.Interfaces;
using SaveState.Core.Models;
using Serilog;

namespace SaveState.Core.Services;

public class TrainerService : ITrainerService
{
    private readonly string _storagePath;
    private readonly ILogger _logger = Log.ForContext<TrainerService>();
    private readonly IMemoryScannerService _scannerService;
    private List<TrainerDefinition> _cache = new();
    private System.Timers.Timer _freezeTimer;

    public TrainerService(IMemoryScannerService scannerService)
    {
        _scannerService = scannerService;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storagePath = Path.Combine(appData, "SaveState", "Trainers");
        Directory.CreateDirectory(_storagePath);

        _freezeTimer = new System.Timers.Timer(100); // 10 ticks per second
        _freezeTimer.Elapsed += async (s, e) => await HandleFreezes();
        _freezeTimer.Start();
    }

    public async Task<List<TrainerDefinition>> GetAllTrainersAsync()
    {
        _cache.Clear();
        var files = Directory.GetFiles(_storagePath, "*.json");
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var trainer = JsonSerializer.Deserialize<TrainerDefinition>(json);
                if (trainer != null) _cache.Add(trainer);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load trainer {File}", file);
            }
        }
        return _cache;
    }

    public async Task CreateCheatAsync(string processName, string cheatName, string address, string type, string defaultValue, bool isFreeze)
    {
        // Find existing or create new
        var trainer = _cache.FirstOrDefault(t => t.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        if (trainer == null)
        {
            trainer = new TrainerDefinition 
            { 
                ProcessName = processName,
                GameTitle = processName // Could improve this
            };
            _cache.Add(trainer);
        }

        var cheat = new CheatDefinition
        {
            Name = cheatName,
            Address = address,
            Type = type,
            Value = defaultValue,
            IsFreeze = isFreeze
        };

        trainer.Cheats.Add(cheat);
        await SaveTrainerAsync(trainer);
    }

    public async Task ToggleCheatAsync(CheatDefinition cheat, bool isActive)
    {
        cheat.IsActive = isActive;
        if (isActive && !cheat.IsFreeze)
        {
            // One-shot write
             await WriteCheatValue(cheat);
             cheat.IsActive = false; // Turn off toggle after one-shot
        }
    }

    private async Task SaveTrainerAsync(TrainerDefinition trainer)
    {
        var path = Path.Combine(_storagePath, $"{trainer.ProcessName}.json");
        var json = JsonSerializer.Serialize(trainer, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    private async Task HandleFreezes()
    {
        var currentProcId = _scannerService.CurrentProcessId;
        if (currentProcId == null) return;

        // Get the current process name to filter trainers
        string? currentProcessName = null;
        try
        {
            var process = System.Diagnostics.Process.GetProcessById(currentProcId.Value);
            currentProcessName = process.ProcessName;
        }
        catch
        {
            return; // Process no longer exists
        }

        foreach (var trainer in _cache)
        {
            // Only apply freezes for the trainer matching the attached process
            if (!trainer.ProcessName.Equals(currentProcessName, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var cheat in trainer.Cheats.Where(c => c.IsActive && c.IsFreeze))
            {
                await WriteCheatValue(cheat);
            }
        }
    }

    private async Task WriteCheatValue(CheatDefinition cheat)
    {
        long targetAddress = 0;
        
        // Use scanner service to resolve pointers or direct addresses
        targetAddress = await _scannerService.ResolvePointerAsync(cheat.Address);
        
        if (targetAddress > 0)
        {
            if (cheat.Type == "float")
            {
                if (float.TryParse(cheat.Value, out float val))
                   _scannerService.WriteFloat(targetAddress, val);
            }
            else
            {
                if (int.TryParse(cheat.Value, out int val))
                   _scannerService.WriteInt32(targetAddress, val);
            }
        }
    }
}
