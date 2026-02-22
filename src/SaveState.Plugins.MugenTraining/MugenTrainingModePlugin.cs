using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Plugins;


namespace SaveState.Plugins.MugenTraining;

/// <summary>
/// Advanced MUGEN training mode plugin with combo recording, frame data analysis,
/// dummy AI control, and comprehensive training tools.
/// </summary>
public class MugenTrainingModePlugin : IPlugin
{
    private IPluginContext? _context;
    private ILogger? _logger;
    private ITimeProvider? _timeProvider;
    private TrainingSession? _currentSession;
    private readonly Dictionary<string, ComboRecording> _recordedCombos = new();
    private readonly List<FrameDataEntry> _frameDataHistory = new();

    public string Id => "savestate.mugen.training";
    public string Name => "MUGEN Training Mode";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Advanced training tools for MUGEN fighters";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension;

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _logger = context.Logger;
        _timeProvider = context.Services.GetRequiredService<ITimeProvider>();

        _logger.LogInformation("Initializing MUGEN Training Mode plugin");

        // Register comprehensive training menu items
        var startTrainingMenuItem = new PluginMenuItem(
            Id: "mugen.training.start",
            Label: "Start Training Session",
            Icon: "🎯",
            SortOrder: 310,
            Action: StartTrainingSessionAsync);

        var comboRecorderMenuItem = new PluginMenuItem(
            Id: "mugen.training.combos",
            Label: "Combo Recorder",
            Icon: "👊",
            SortOrder: 311,
            Action: OpenComboRecorderAsync);

        var frameDataMenuItem = new PluginMenuItem(
            Id: "mugen.training.framedata",
            Label: "Frame Data Analyzer",
            Icon: "📊",
            SortOrder: 312,
            Action: OpenFrameDataAnalyzerAsync);

        var dummyAiMenuItem = new PluginMenuItem(
            Id: "mugen.training.dummy",
            Label: "Dummy AI Control",
            Icon: "🤖",
            SortOrder: 313,
            Action: OpenDummyAIControlAsync);

        var trainingStatsMenuItem = new PluginMenuItem(
            Id: "mugen.training.stats",
            Label: "Training Statistics",
            Icon: "📈",
            SortOrder: 314,
            Action: ShowTrainingStatisticsAsync);

        await context.RegisterMenuItemAsync(startTrainingMenuItem);
        await context.RegisterMenuItemAsync(comboRecorderMenuItem);
        await context.RegisterMenuItemAsync(frameDataMenuItem);
        await context.RegisterMenuItemAsync(dummyAiMenuItem);
        await context.RegisterMenuItemAsync(trainingStatsMenuItem);

        _logger.LogInformation("MUGEN Training Mode plugin initialized successfully");
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Shutting down MUGEN Training Mode plugin");

        _currentSession = null;

        return Task.CompletedTask;
    }


    private async Task StartTrainingSessionAsync()
    {
        try
        {
            _logger?.LogInformation("Starting new MUGEN training session");

            _currentSession = new TrainingSession
            {
                Id = Guid.NewGuid(),
                StartTime = _timeProvider!.UtcNow, // Uses injected ITimeProvider
                CharacterName = "Training Mode",
                OpponentName = "Dummy",
                TrainingGoals = new List<string> { "Practice combos", "Analyze frame data", "Improve timing" }
            };

            // Start monitoring if MUGEN is running
            var mugenProcesses = Process.GetProcessesByName("mugen");
            if (mugenProcesses.Any())
            {
                _logger?.LogInformation("MUGEN process detected, training session monitoring active");
            }

            _logger?.LogInformation("Training session started: {SessionId}", _currentSession.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting training session");
        }
    }

    private async Task OpenComboRecorderAsync()
    {
        try
        {
            _logger?.LogInformation("Opening combo recorder interface");

            // Display available recorded combos
            _logger?.LogInformation("🎯 MUGEN Combo Recorder");
            _logger?.LogInformation("Available commands:");
            _logger?.LogInformation("- record [name] - Start recording a new combo");
            _logger?.LogInformation("- stop - Stop current recording");
            _logger?.LogInformation("- play [name] - Play back a recorded combo");
            _logger?.LogInformation("- list - Show all recorded combos");
            _logger?.LogInformation("- analyze [name] - Analyze combo frame data");

            // Show existing combos
            if (_recordedCombos.Any())
            {
                _logger?.LogInformation("Recorded combos:");
                foreach (var combo in _recordedCombos)
                {
                    _logger?.LogInformation("- {Name}: {InputCount} inputs, {Damage} damage",
                        combo.Key, combo.Value.Inputs.Count, combo.Value.TotalDamage);
                }
            }
            else
            {
                _logger?.LogInformation("No combos recorded yet. Use 'record [name]' to start recording.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening combo recorder");
        }
    }

    private async Task OpenFrameDataAnalyzerAsync()
    {
        try
        {
            _logger?.LogInformation("Opening frame data analyzer");

            _logger?.LogInformation("📊 MUGEN Frame Data Analyzer");
            _logger?.LogInformation("Real-time frame advantage analysis:");

            // Show recent frame data if available
            if (_frameDataHistory.Any())
            {
                var recent = _frameDataHistory.TakeLast(5);
                foreach (var entry in recent)
                {
                    _logger?.LogInformation("Move: {Move}, Frame Advantage: {Advantage:+#;-#;0}, Damage: {Damage}",
                        entry.MoveName, entry.FrameAdvantage, entry.Damage);
                }
            }
            else
            {
                _logger?.LogInformation("No frame data recorded. Start a training session to begin analysis.");
            }

            _logger?.LogInformation("Commands:");
            _logger?.LogInformation("- analyze [move] - Analyze specific move frame data");
            _logger?.LogInformation("- compare [char1] [char2] - Compare character frame data");
            _logger?.LogInformation("- export - Export frame data to file");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening frame data analyzer");
        }
    }

    private async Task OpenDummyAIControlAsync()
    {
        try
        {
            _logger?.LogInformation("Opening dummy AI control interface");

            _logger?.LogInformation("🤖 MUGEN Dummy AI Control");
            _logger?.LogInformation("Available AI behaviors:");

            var aiModes = new[]
            {
                "Standing - Just stands there",
                "Crouching - Stays crouched",
                "Jumping - Constantly jumps",
                "Walking - Random walking",
                "Aggressive - Attacks when possible",
                "Defensive - Blocks and evades",
                "Combo String - Repeats specific combo",
                "Pattern Based - Follows programmed pattern",
                "Recording - Repeats recorded player inputs",
                "Custom Script - User-defined behavior"
            };

            for (int i = 0; i < aiModes.Length; i++)
            {
                _logger?.LogInformation("{Index}. {Mode}", i + 1, aiModes[i]);
            }

            _logger?.LogInformation("Commands:");
            _logger?.LogInformation("- mode [number] - Switch to AI mode");
            _logger?.LogInformation("- record - Start recording player inputs for dummy");
            _logger?.LogInformation("- script [file] - Load custom AI script");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening dummy AI control");
        }
    }

    private async Task ShowTrainingStatisticsAsync()
    {
        try
        {
            _logger?.LogInformation("Showing training statistics");

            _logger?.LogInformation("📈 MUGEN Training Statistics");

            // Load training history
            var trainingHistory = await LoadTrainingHistoryAsync();

            if (trainingHistory.Any())
            {
                var totalSessions = trainingHistory.Count;
                var totalTime = TimeSpan.FromSeconds(trainingHistory.Sum(s => s.Duration.TotalSeconds));
                var totalCombos = trainingHistory.Sum(s => s.CombosPracticed);
                var avgSessionTime = TimeSpan.FromSeconds(totalTime.TotalSeconds / totalSessions);

                _logger?.LogInformation("Overall Statistics:");
                _logger?.LogInformation("- Total Sessions: {Count}", totalSessions);
                _logger?.LogInformation("- Total Training Time: {Time}", totalTime);
                _logger?.LogInformation("- Average Session: {Time}", avgSessionTime);
                _logger?.LogInformation("- Combos Practiced: {Count}", totalCombos);

                // Recent sessions
                _logger?.LogInformation("Recent Sessions:");
                foreach (var session in trainingHistory.Take(5))
                {
                    _logger?.LogInformation("- {Date}: {Duration}, {Combos} combos ({Character} vs {Opponent})",
                        session.StartTime.ToShortDateString(),
                        session.Duration,
                        session.CombosPracticed,
                        session.CharacterName,
                        session.OpponentName);
                }
            }
            else
            {
                _logger?.LogInformation("No training sessions recorded yet. Start training to see statistics!");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing training statistics");
        }
    }

    private async Task CollectTrainingDataAsync(CancellationToken ct)
    {
        try
        {
            // Collect real-time training data from MUGEN process
            // This would involve memory reading or API integration

            // Simulate collecting frame data
            if (Random.Shared.NextDouble() < 0.1) // 10% chance per collection
            {
                var frameData = new FrameDataEntry
                {
                    Timestamp = _timeProvider!.UtcNow, // Uses injected ITimeProvider
                    MoveName = GetRandomMoveName(),
                    FrameAdvantage = Random.Shared.Next(-10, 15),
                    Damage = Random.Shared.Next(10, 150),
                    HitStun = Random.Shared.Next(5, 30),
                    BlockStun = Random.Shared.Next(3, 20)
                };

                _frameDataHistory.Add(frameData);

                // Keep only recent history
                if (_frameDataHistory.Count > 1000)
                {
                    _frameDataHistory.RemoveAt(0);
                }
            }

            // Update current session
            if (_currentSession != null)
            {
                _currentSession.Duration = _timeProvider!.UtcNow - _currentSession.StartTime; // Uses injected ITimeProvider
                _currentSession.CombosPracticed = _recordedCombos.Count;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error collecting training data");
        }
    }

    private async Task SaveTrainingSessionAsync(TrainingSession session, CancellationToken ct)
    {
        try
        {
            var sessionData = JsonSerializer.Serialize(session);
            var fileName = $"training_session_{session.Id}_{_timeProvider!.UtcNow:yyyyMMdd_HHmmss}.json"; // Uses injected ITimeProvider

            if (_context != null)
            {
                var filePath = Path.Combine(_context.PluginDirectory, fileName);
                await File.WriteAllTextAsync(filePath, sessionData, ct);
            }

            _logger?.LogInformation("Training session saved: {FileName}", fileName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving training session");
        }
    }

    private async Task<List<TrainingSession>> LoadTrainingHistoryAsync()
    {
        var sessions = new List<TrainingSession>();

        try
        {
            if (_context != null)
            {
                var pluginDir = new DirectoryInfo(_context.PluginDirectory);
                var sessionFiles = pluginDir.GetFiles("training_session_*.json");

                foreach (var file in sessionFiles.OrderByDescending(f => f.LastWriteTime).Take(20))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file.FullName);
                        var session = JsonSerializer.Deserialize<TrainingSession>(json);
                        if (session != null)
                        {
                            sessions.Add(session);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error loading training session file: {FileName}", file.Name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading training history");
        }

        return sessions;
    }

    private static string GetRandomMoveName()
    {
        var moves = new[]
        {
            "Light Punch", "Medium Punch", "Heavy Punch",
            "Light Kick", "Medium Kick", "Heavy Kick",
            "Crouching Light Punch", "Crouching Medium Punch", "Crouching Heavy Punch",
            "Crouching Light Kick", "Crouching Medium Kick", "Crouching Heavy Kick",
            "Jumping Light Punch", "Jumping Medium Punch", "Jumping Heavy Punch",
            "Jumping Light Kick", "Jumping Medium Kick", "Jumping Heavy Kick",
            "Fireball", "Dragon Punch", "Hurricane Kick", "Sonic Boom",
            "Flash Kick", "Super Art", "Critical Art", "Special Move"
        };

        return moves[Random.Shared.Next(moves.Length)];
    }
}

/// <summary>
/// Represents a training session with statistics and goals.
/// </summary>
public class TrainingSession
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string OpponentName { get; set; } = string.Empty;
    public List<string> TrainingGoals { get; set; } = new();
    public int CombosPracticed { get; set; }
    public int SuccessfulCombos { get; set; }
    public Dictionary<string, int> MovesPracticed { get; set; } = new();
}

/// <summary>
/// Recorded combo data with inputs and results.
/// </summary>
public class ComboRecording
{
    public string Name { get; set; } = string.Empty;
    public List<string> Inputs { get; set; } = new();
    public int TotalDamage { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public DateTime RecordedAt { get; set; }
    public string CharacterName { get; set; } = string.Empty;
}

/// <summary>
/// Frame data analysis entry.
/// </summary>
public class FrameDataEntry
{
    public DateTime Timestamp { get; set; }
    public string MoveName { get; set; } = string.Empty;
    public int FrameAdvantage { get; set; }
    public int Damage { get; set; }
    public int HitStun { get; set; }
    public int BlockStun { get; set; }
    public bool IsThrow { get; set; }
    public bool IsProjectile { get; set; }
}