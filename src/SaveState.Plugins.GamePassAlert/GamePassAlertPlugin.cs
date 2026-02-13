using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Plugins;
using System.Text.Json;

namespace SaveState.Plugins.GamePassAlert;

/// <summary>
/// Plugin that alerts when Game Pass titles are leaving the service.
/// </summary>
public sealed class GamePassAlertPlugin : IPlugin
{
    private IPluginContext? _context;
    private ITimeProvider _timeProvider = null!;
    private readonly HttpClient _httpClient = new();
    private Timer? _checkTimer;
    private GamePassAlertSettings _settings = new();
    private List<LeavingGame> _leavingGames = new();

    public string Id => "gamepass-leaving-soon";
    public string Name => "Game Pass Leaving Soon";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Alert when Game Pass titles are leaving the service with countdown and priority actions.";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension;

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _timeProvider = context.Services.GetRequiredService<ITimeProvider>();
        _context.Logger.LogInformation("Game Pass Leaving Soon plugin initialized");

        LoadSettings();

        if (_settings.Enabled)
        {
            // Initial check
            await CheckLeavingGamesAsync();

            // Set up periodic check (every 6 hours)
            _checkTimer = new Timer(async _ => await CheckLeavingGamesAsync(), null,
                TimeSpan.Zero, TimeSpan.FromHours(6));
        }
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _checkTimer?.Dispose();
        _httpClient.Dispose();
        SaveSettings();

        return Task.CompletedTask;
    }

    private async Task CheckLeavingGamesAsync()
    {
        try
        {
            _context?.Logger.LogInformation("Checking for Game Pass games leaving soon...");

            // Fetch leaving soon list from Xbox API
            var leavingGames = await FetchLeavingGamesAsync();

            // Check for new games or updated dates
            foreach (var game in leavingGames)
            {
                var existing = _leavingGames.FirstOrDefault(g => g.Title == game.Title);
                if (existing == null)
                {
                    // New game added to leaving list
                    _context?.Logger.LogWarning("New game leaving Game Pass: {Title} on {Date}",
                        game.Title, game.LeavingDate.ToShortDateString());

                    SendNotification(game, "New game leaving Game Pass!");
                }
                else if (existing.LeavingDate != game.LeavingDate)
                {
                    // Date changed
                    _context?.Logger.LogInformation("Leaving date updated for {Title}: {OldDate} -> {NewDate}",
                        game.Title, existing.LeavingDate.ToShortDateString(), game.LeavingDate.ToShortDateString());
                }

                // Check for upcoming notifications
                var now = _timeProvider.Now;
                var daysRemaining = (game.LeavingDate - now).Days;
                if (daysRemaining <= 7 && !game.NotifiedAt7Days)
                {
                    SendNotification(game, $"Leaving in {daysRemaining} days!");
                    game.NotifiedAt7Days = true;
                }
                else if (daysRemaining <= 3 && !game.NotifiedAt3Days)
                {
                    SendNotification(game, $"Leaving in {daysRemaining} days!");
                    game.NotifiedAt3Days = true;
                }
                else if (daysRemaining <= 1 && !game.NotifiedAt1Day)
                {
                    SendNotification(game, "Leaving tomorrow!");
                    game.NotifiedAt1Day = true;
                }
            }

            _leavingGames = leavingGames;
            SaveSettings();
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to check leaving games");
        }
    }

    private async Task<List<LeavingGame>> FetchLeavingGamesAsync()
    {
        try
        {
            // In a real implementation, this would call the Xbox Game Pass API
            // For now, we'll use a mock implementation
            var url = "https://catalog.gamepass.com/sigls/v2?id=fdd9e2a7-0fee-49f6-ad69-4354098401ff&language=en-us&market=US";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _context?.Logger.LogWarning("Failed to fetch Game Pass catalog: {StatusCode}", response.StatusCode);
                return new List<LeavingGame>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var catalog = JsonSerializer.Deserialize<GamePassCatalog>(json);

            var leavingGames = new List<LeavingGame>();

            if (catalog?.Products != null)
            {
                foreach (var product in catalog.Products)
                {
                    var now = _timeProvider.Now;
                    if (product.LeavingDate.HasValue && product.LeavingDate.Value > now)
                    {
                        leavingGames.Add(new LeavingGame
                        {
                            Title = product.Title ?? "Unknown",
                            LeavingDate = product.LeavingDate.Value,
                            ProductId = product.ProductId ?? ""
                        });
                    }
                }
            }

            _context?.Logger.LogInformation("Found {Count} games leaving Game Pass", leavingGames.Count);
            return leavingGames;
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to fetch leaving games");
            return new List<LeavingGame>();
        }
    }

    private void SendNotification(LeavingGame game, string message)
    {
        _context?.Logger.LogWarning("Game Pass Alert: {Game} - {Message}", game.Title, message);
        _context?.ReportProgress($"{game.Title}: {message}", 1.0f);

        // In a real implementation, this would show a system notification
    }

    private void LoadSettings()
    {
        try
        {
            var settingsPath = Path.Combine(_context?.DataDirectory ?? ".", "settings.json");
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var data = JsonSerializer.Deserialize<GamePassAlertData>(json);
                if (data != null)
                {
                    _settings = data.Settings;
                    _leavingGames = data.LeavingGames;
                }
            }
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to load settings");
        }
    }

    private void SaveSettings()
    {
        try
        {
            var settingsPath = Path.Combine(_context?.DataDirectory ?? ".", "settings.json");
            var directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var data = new GamePassAlertData
            {
                Settings = _settings,
                LeavingGames = _leavingGames
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to save settings");
        }
    }
}

/// <summary>
/// Settings for Game Pass Alert plugin.
/// </summary>
public sealed class GamePassAlertSettings
{
    public bool Enabled { get; set; } = true;
    public bool NotifyAt7Days { get; set; } = true;
    public bool NotifyAt3Days { get; set; } = true;
    public bool NotifyAt1Day { get; set; } = true;
    public int CheckIntervalHours { get; set; } = 6;
}

/// <summary>
/// Represents a game leaving Game Pass.
/// </summary>
public sealed class LeavingGame
{
    public string Title { get; set; } = string.Empty;
    public DateTime LeavingDate { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public bool NotifiedAt7Days { get; set; }
    public bool NotifiedAt3Days { get; set; }
    public bool NotifiedAt1Day { get; set; }
}

/// <summary>
/// Combined settings and data for persistence.
/// </summary>
public sealed class GamePassAlertData
{
    public GamePassAlertSettings Settings { get; set; } = new();
    public List<LeavingGame> LeavingGames { get; set; } = new();
}

/// <summary>
/// Game Pass catalog response (simplified).
/// </summary>
public sealed class GamePassCatalog
{
    public List<GamePassProduct>? Products { get; set; }
}

/// <summary>
/// Game Pass product entry.
/// </summary>
public sealed class GamePassProduct
{
    public string? ProductId { get; set; }
    public string? Title { get; set; }
    public DateTime? LeavingDate { get; set; }
}
