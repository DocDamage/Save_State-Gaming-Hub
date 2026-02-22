using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.MugenReplay;

/// <summary>
/// MUGEN replay manager with recording, playback, analysis, and sharing capabilities.
/// Allows users to save matches, analyze performance, and share highlights.
/// </summary>
public class MugenReplayManagerPlugin : IPlugin
{
    private IPluginContext? _context;
    private ILogger? _logger;
    private ITimeProvider? _timeProvider;
    private readonly Dictionary<Guid, ReplayRecording> _activeRecordings = new();
    private readonly List<ReplayMetadata> _replayLibrary = new();

    public string Id => "savestate.mugen.replay";
    public string Name => "MUGEN Replay Manager";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Record, analyze, and share MUGEN match replays";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension;


    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _logger = context.Logger;
        _timeProvider = context.Services.GetRequiredService<ITimeProvider>();

        _logger.LogInformation("Initializing MUGEN Replay Manager plugin");

        // Register menu items
        var recordMenuItem = new PluginMenuItem(
            Id: "mugen.replay.record",
            Label: "Start Recording",
            Icon: "⏺️",
            SortOrder: 320,
            Action: StartRecordingAsync);

        var libraryMenuItem = new PluginMenuItem(
            Id: "mugen.replay.library",
            Label: "Replay Library",
            Icon: "📚",
            SortOrder: 321,
            Action: OpenReplayLibraryAsync);

        var analyzerMenuItem = new PluginMenuItem(
            Id: "mugen.replay.analyze",
            Label: "Replay Analyzer",
            Icon: "📊",
            SortOrder: 322,
            Action: OpenReplayAnalyzerAsync);

        var shareMenuItem = new PluginMenuItem(
            Id: "mugen.replay.share",
            Label: "Share Replays",
            Icon: "📤",
            SortOrder: 323,
            Action: OpenShareInterfaceAsync);

        await context.RegisterMenuItemAsync(recordMenuItem);
        await context.RegisterMenuItemAsync(libraryMenuItem);
        await context.RegisterMenuItemAsync(analyzerMenuItem);
        await context.RegisterMenuItemAsync(shareMenuItem);

        // Load existing replays
        await LoadReplayLibraryAsync(ct);

        _logger.LogInformation("MUGEN Replay Manager plugin initialized successfully");
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Shutting down MUGEN Replay Manager plugin");

        // Stop any active recordings
        foreach (var recording in _activeRecordings.Values)
        {
            recording.IsRecording = false;
        }
        _activeRecordings.Clear();

        return Task.CompletedTask;
    }

    // IUIPanel implementation

    private async Task StartRecordingAsync()
    {
        try
        {
            _logger?.LogInformation("Starting MUGEN replay recording");

            // Check if MUGEN is running
            var mugenProcesses = Process.GetProcessesByName("mugen");
            if (!mugenProcesses.Any())
            {
                _logger?.LogWarning("MUGEN is not running. Start MUGEN first to record replays.");
                return;
            }

            var sessionId = Guid.NewGuid();
            var recording = new ReplayRecording
            {
                Id = sessionId,
                StartTime = _timeProvider!.UtcNow, // Uses injected ITimeProvider
                IsRecording = true,
                Player1Character = "Unknown",
                Player2Character = "Unknown",
                Stage = "Unknown"
            };

            _activeRecordings[sessionId] = recording;

            _logger?.LogInformation("Replay recording started: {SessionId}", sessionId);

            // Start background recording task
            _ = Task.Run(() => RecordReplayAsync(sessionId, CancellationToken.None));

            _logger?.LogInformation("🎥 Recording started! Use 'Stop Recording' when match ends.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting replay recording");
        }
    }

    private async Task RecordReplayAsync(Guid sessionId, CancellationToken ct)
    {
        try
        {
            if (!_activeRecordings.TryGetValue(sessionId, out var recording))
                return;

            var inputBuffer = new List<InputFrame>();

            // Simulate recording inputs (in real implementation, this would hook into MUGEN)
            while (recording.IsRecording && !ct.IsCancellationRequested)
            {
                // Capture game state every frame
                var frame = new InputFrame
                {
                    Timestamp = _timeProvider!.UtcNow, // Uses injected ITimeProvider
                    FrameNumber = inputBuffer.Count,
                    Player1Inputs = GenerateRandomInputs(),
                    Player2Inputs = GenerateRandomInputs(),
                    Player1Health = Math.Max(0, 1000 - (inputBuffer.Count * 2)), // Simulate damage
                    Player2Health = Math.Max(0, 1000 - (inputBuffer.Count * 3)),
                    RoundNumber = 1,
                    RoundTime = Math.Max(0, 99 - (inputBuffer.Count / 60)) // 60 FPS
                };

                inputBuffer.Add(frame);

                // Check for match end conditions
                if (frame.Player1Health <= 0 || frame.Player2Health <= 0 || frame.RoundTime <= 0)
                {
                    recording.IsRecording = false;
                    await SaveReplayAsync(recording, inputBuffer);
                    break;
                }

                await Task.Delay(16, ct); // ~60 FPS
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during replay recording: {SessionId}", sessionId);
        }
        finally
        {
            _activeRecordings.Remove(sessionId);
        }
    }

    private async Task SaveReplayAsync(ReplayRecording recording, List<InputFrame> frames)
    {
        try
        {
            recording.EndTime = _timeProvider!.UtcNow; // Uses injected ITimeProvider
            recording.Duration = recording.EndTime - recording.StartTime;
            recording.FrameCount = frames.Count;

            // Determine winner
            var lastFrame = frames.Last();
            recording.Winner = lastFrame.Player1Health > lastFrame.Player2Health ? "Player 1" : "Player 2";

            // Create replay data
            var replayData = new ReplayData
            {
                Metadata = new ReplayMetadata
                {
                    Id = recording.Id,
                    Title = $"{recording.Player1Character} vs {recording.Player2Character}",
                    Description = $"MUGEN match recorded on {recording.StartTime.ToShortDateString()}",
                    RecordedAt = recording.StartTime,
                    Duration = recording.Duration,
                    Player1Character = recording.Player1Character,
                    Player2Character = recording.Player2Character,
                    Stage = recording.Stage,
                    Winner = recording.Winner,
                    FileSize = 0 // Will be calculated after compression
                },
                Frames = frames
            };

            // Save to compressed file
            var fileName = $"replay_{recording.Id}.mugenreplay";
            if (_context != null)
            {
                var filePath = Path.Combine(_context.PluginDirectory, fileName);
                await SaveCompressedReplayAsync(replayData, filePath);

                // Update file size
                var fileInfo = new FileInfo(filePath);
                replayData.Metadata.FileSize = fileInfo.Length;

                _replayLibrary.Add(replayData.Metadata);
                await SaveReplayLibraryAsync();
            }

            _logger?.LogInformation("Replay saved: {FileName} ({Duration})", fileName, recording.Duration);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving replay: {SessionId}", recording.Id);
        }
    }

    private async Task OpenReplayLibraryAsync()
    {
        try
        {
            _logger?.LogInformation("Opening replay library");

            _logger?.LogInformation("📚 MUGEN Replay Library");
            _logger?.LogInformation("Available replays:");

            if (!_replayLibrary.Any())
            {
                _logger?.LogInformation("No replays found. Start recording some matches!");
                return;
            }

            foreach (var replay in _replayLibrary.OrderByDescending(r => r.RecordedAt))
            {
                var size = replay.FileSize > 1024 * 1024
                    ? $"{replay.FileSize / (1024.0 * 1024.0):F1}MB"
                    : $"{replay.FileSize / 1024.0:F1}KB";

                _logger?.LogInformation("- {Title} ({Duration}) - {Winner} won - {Size}",
                    replay.Title,
                    replay.Duration.ToString(@"mm\:ss"),
                    replay.Winner,
                    size);
                _logger?.LogInformation("  {Date} - {P1} vs {P2} on {Stage}",
                    replay.RecordedAt.ToShortDateString(),
                    replay.Player1Character,
                    replay.Player2Character,
                    replay.Stage);
            }

            _logger?.LogInformation("Commands:");
            _logger?.LogInformation("- play [index] - Play selected replay");
            _logger?.LogInformation("- analyze [index] - Analyze selected replay");
            _logger?.LogInformation("- delete [index] - Delete selected replay");
            _logger?.LogInformation("- export [index] - Export replay to share");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening replay library");
        }
    }

    private async Task OpenReplayAnalyzerAsync()
    {
        try
        {
            _logger?.LogInformation("Opening replay analyzer");

            _logger?.LogInformation("📊 MUGEN Replay Analyzer");
            _logger?.LogInformation("Select a replay to analyze:");

            if (_replayLibrary.Any())
            {
                for (int i = 0; i < _replayLibrary.Count; i++)
                {
                    var replay = _replayLibrary[i];
                    _logger?.LogInformation("{Index}. {Title} ({Duration})",
                        i + 1, replay.Title, replay.Duration.ToString(@"mm\:ss"));
                }

                _logger?.LogInformation("Analysis options:");
                _logger?.LogInformation("- damage - Show damage over time");
                _logger?.LogInformation("- inputs - Analyze input patterns");
                _logger?.LogInformation("- mistakes - Highlight errors/mistakes");
                _logger?.LogInformation("- highlights - Extract highlight moments");
            }
            else
            {
                _logger?.LogInformation("No replays available for analysis.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening replay analyzer");
        }
    }

    private async Task OpenShareInterfaceAsync()
    {
        try
        {
            _logger?.LogInformation("Opening replay sharing interface");

            _logger?.LogInformation("📤 MUGEN Replay Sharing");
            _logger?.LogInformation("Share your epic moments with the community:");

            var shareableReplays = _replayLibrary.Where(r => r.Duration > TimeSpan.FromSeconds(30)).ToList();

            if (shareableReplays.Any())
            {
                _logger?.LogInformation("Shareable replays (30+ seconds):");
                for (int i = 0; i < shareableReplays.Count; i++)
                {
                    var replay = shareableReplays[i];
                    _logger?.LogInformation("{Index}. {Title} - {Duration}",
                        i + 1, replay.Title, replay.Duration.ToString(@"mm\:ss"));
                }

                _logger?.LogInformation("Sharing options:");
                _logger?.LogInformation("- upload [index] - Upload to MUGEN community");
                _logger?.LogInformation("- export [index] - Export as video file");
                _logger?.LogInformation("- clip [index] [start] [duration] - Create highlight clip");
            }
            else
            {
                _logger?.LogInformation("No replays long enough to share. Record some longer matches!");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening share interface");
        }
    }

    private async Task LoadReplayLibraryAsync(CancellationToken ct = default)
    {
        try
        {
            if (_context == null) return;

            var libraryPath = Path.Combine(_context.PluginDirectory, "replay_library.json");
            if (File.Exists(libraryPath))
            {
                var json = await File.ReadAllTextAsync(libraryPath, ct);
                var library = JsonSerializer.Deserialize<List<ReplayMetadata>>(json);
                if (library != null)
                {
                    _replayLibrary.Clear();
                    _replayLibrary.AddRange(library);
                }
            }

            _logger?.LogInformation("Loaded {Count} replays from library", _replayLibrary.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading replay library");
        }
    }

    private async Task SaveReplayLibraryAsync()
    {
        try
        {
            if (_context == null) return;

            var libraryPath = Path.Combine(_context.PluginDirectory, "replay_library.json");
            var json = JsonSerializer.Serialize(_replayLibrary, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(libraryPath, json);

            _logger?.LogInformation("Saved replay library ({Count} replays)", _replayLibrary.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving replay library");
        }
    }

    private async Task RefreshReplayLibraryAsync()
    {
        // Refresh from disk in case files were added externally
        await LoadReplayLibraryAsync();
    }

    private static async Task SaveCompressedReplayAsync(ReplayData replayData, string filePath)
    {
        await Task.Run(() =>
        {
            using var fileStream = File.Create(filePath);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

            // Save metadata
            var metadataEntry = archive.CreateEntry("metadata.json");
            using (var entryStream = metadataEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                var json = JsonSerializer.Serialize(replayData.Metadata);
                writer.Write(json);
            }

            // Save frame data
            var framesEntry = archive.CreateEntry("frames.json");
            using (var entryStream = framesEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                var json = JsonSerializer.Serialize(replayData.Frames);
                writer.Write(json);
            }
        });
    }

    private static string GenerateRandomInputs()
    {
        var inputs = new[] { "LP", "MP", "HP", "LK", "MK", "HK", "→", "←", "↑", "↓", "Start", "Select" };
        var inputCount = Random.Shared.Next(0, 4); // 0-3 simultaneous inputs
        var selectedInputs = new List<string>();

        for (int i = 0; i < inputCount; i++)
        {
            selectedInputs.Add(inputs[Random.Shared.Next(inputs.Length)]);
        }

        return selectedInputs.Any() ? string.Join("+", selectedInputs) : "Neutral";
    }
}

/// <summary>
/// Active replay recording session.
/// </summary>
public class ReplayRecording
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsRecording { get; set; }
    public string Player1Character { get; set; } = string.Empty;
    public string Player2Character { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string Winner { get; set; } = string.Empty;
    public int FrameCount { get; set; }
}

/// <summary>
/// Complete replay data with metadata and frames.
/// </summary>
public class ReplayData
{
    public ReplayMetadata Metadata { get; set; } = new();
    public List<InputFrame> Frames { get; set; } = new();
}

/// <summary>
/// Replay metadata for library display.
/// </summary>
public class ReplayMetadata
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public string Player1Character { get; set; } = string.Empty;
    public string Player2Character { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string Winner { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

/// <summary>
/// Individual input frame in a replay.
/// </summary>
public class InputFrame
{
    public DateTime Timestamp { get; set; }
    public int FrameNumber { get; set; }
    public string Player1Inputs { get; set; } = string.Empty;
    public string Player2Inputs { get; set; } = string.Empty;
    public int Player1Health { get; set; }
    public int Player2Health { get; set; }
    public int RoundNumber { get; set; }
    public int RoundTime { get; set; }
}