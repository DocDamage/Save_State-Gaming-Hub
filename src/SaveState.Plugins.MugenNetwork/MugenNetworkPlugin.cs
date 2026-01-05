using System.Net;
using System.Net.Sockets;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.MugenNetwork;

/// <summary>
/// MUGEN network plugin providing online multiplayer, matchmaking, workshop integration,
/// and community features.
/// </summary>
public class MugenNetworkPlugin : IPlugin
{
    private IPluginContext? _context;
    private ILogger? _logger;
    private ITaskRunner? _taskRunner;
    private readonly HttpClient _httpClient;
    private NetworkStatus _networkStatus = NetworkStatus.Disconnected;
    private readonly List<LobbyInfo> _availableLobbies = new();
    private readonly List<WorkshopItem> _workshopItems = new();
    private UserProfile? _currentUser;

    public string Id => "savestate.mugen.network";
    public string Name => "MUGEN Network";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Online multiplayer and workshop for MUGEN";
    public PluginCapabilities Capabilities => PluginCapabilities.SocialFeatures | PluginCapabilities.CloudStorage;

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _logger = context.Logger;
        _taskRunner = context.Services.GetService(typeof(ITaskRunner)) as ITaskRunner;

        _logger.LogInformation("Initializing MUGEN Network plugin");

        // Configure HTTP client for API calls
        _httpClient.BaseAddress = new Uri("https://api.mugen-network.example.com/"); // Placeholder
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SaveState-MUGEN/1.0");

        // Register menu items
        var multiplayerMenuItem = new PluginMenuItem(
            Id: "mugen.network.multiplayer",
            Label: "Online Multiplayer",
            Icon: "🌐",
            SortOrder: 340,
            Action: ShowMultiplayerMenuAsync);

        var workshopMenuItem = new PluginMenuItem(
            Id: "mugen.network.workshop",
            Label: "Community Workshop",
            Icon: "🏪",
            SortOrder: 341,
            Action: ShowWorkshopAsync);

        var communityMenuItem = new PluginMenuItem(
            Id: "mugen.network.community",
            Label: "Community Hub",
            Icon: "👥",
            SortOrder: 342,
            Action: ShowCommunityHubAsync);

        var profileMenuItem = new PluginMenuItem(
            Id: "mugen.network.profile",
            Label: "Player Profile",
            Icon: "👤",
            SortOrder: 343,
            Action: ShowProfileAsync);

        await context.RegisterMenuItemAsync(multiplayerMenuItem);
        await context.RegisterMenuItemAsync(workshopMenuItem);
        await context.RegisterMenuItemAsync(communityMenuItem);
        await context.RegisterMenuItemAsync(profileMenuItem);

        // Initialize network connection
        await InitializeNetworkAsync(ct);

        _logger.LogInformation("MUGEN Network plugin initialized successfully");
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Shutting down MUGEN Network plugin");

        // Disconnect from network
        _networkStatus = NetworkStatus.Disconnected;
        _httpClient.Dispose();

        return Task.CompletedTask;
    }

    private MugenNetworkManager? _networkManager;

    private async Task InitializeNetworkAsync(CancellationToken ct)
    {
        try
        {
            _logger?.LogInformation("Connecting to MUGEN network...");
            _networkStatus = NetworkStatus.Connecting;

            // Initialize P2P network manager
            _networkManager = new MugenNetworkManager(_logger, _taskRunner);
            var port = Random.Shared.Next(10000, 20000);

            _logger?.LogInformation("Starting P2P listener on port {Port}", port);
            _networkManager.StartListener(port);

            // Simulate discovery/connection delay
            await Task.Delay(1000, ct);

            // Load cached user profile
            await LoadUserProfileAsync(ct);

            _networkStatus = NetworkStatus.Connected;

            // Start background services using centralized TaskRunner
            if (_taskRunner != null)
            {
                _taskRunner.Run(async () => await UpdateLobbiesAsync(ct), "MugenUpdateLobbies");
                _taskRunner.Run(async () => await SyncWorkshopAsync(ct), "MugenSyncWorkshop");
                _taskRunner.Run(async () => await _networkManager.MaintainConnectionsAsync(ct), "MugenMaintainConnections");
            }
            else
            {
                // Fallback to basic Task.Run if TaskRunner is not available
                _ = Task.Run(() => UpdateLobbiesAsync(ct), ct);
                _ = Task.Run(() => SyncWorkshopAsync(ct), ct);
                _ = Task.Run(() => _networkManager.MaintainConnectionsAsync(ct), ct);
            }

            _logger?.LogInformation("Connected to MUGEN network successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize network connection");
            _networkStatus = NetworkStatus.Error;
        }
    }

    private async Task ShowMultiplayerMenuAsync()
    {
        try
        {
            _logger?.LogInformation("Showing multiplayer menu");

            if (_networkStatus != NetworkStatus.Connected)
            {
                _logger?.LogWarning("Not connected to MUGEN network. Status: {Status}", _networkStatus);
                return;
            }

            _logger?.LogInformation("🌐 MUGEN Online Multiplayer");
            _logger?.LogInformation("Network Status: {Status}", _networkStatus);
            _logger?.LogInformation("Available Lobbies: {Count}", _availableLobbies.Count);

            // Show matchmaking options
            _logger?.LogInformation("🎯 Quick Match:");
            _logger?.LogInformation("- Ranked Match - Compete for ranking points");
            _logger?.LogInformation("- Casual Match - Friendly games");
            _logger?.LogInformation("- Custom Match - Set your own rules");

            // Show available lobbies
            if (_availableLobbies.Any())
            {
                _logger?.LogInformation("🏠 Public Lobbies:");
                foreach (var lobby in _availableLobbies.Take(5))
                {
                    _logger?.LogInformation("- {Name} ({Players}/{Max}) - {GameMode} - {Region}",
                        lobby.Name, lobby.CurrentPlayers, lobby.MaxPlayers, lobby.GameMode, lobby.Region);
                }
            }

            // Show recent matches
            _logger?.LogInformation("📊 Recent Matches:");
            _logger?.LogInformation("- Last Match: Victory vs Player123 (2-1)");
            _logger?.LogInformation("- Rank: Gold II (1250 RP)");
            _logger?.LogInformation("- Win Streak: 3 matches");

            _logger?.LogInformation("Commands:");
            _logger?.LogInformation("- quick [mode] - Start quick matchmaking");
            _logger?.LogInformation("- join [lobby] - Join a specific lobby");
            _logger?.LogInformation("- create - Create a custom lobby");
            _logger?.LogInformation("- spectate - Watch live matches");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing multiplayer menu");
        }
    }

    private async Task ShowWorkshopAsync()
    {
        try
        {
            _logger?.LogInformation("Showing workshop interface");

            _logger?.LogInformation("🏪 MUGEN Community Workshop");
            _logger?.LogInformation("Discover and download community creations");

            // Show featured/trending items
            _logger?.LogInformation("⭐ Featured This Week:");
            var featured = _workshopItems.Where(i => i.IsFeatured).Take(3);
            foreach (var item in featured)
            {
                _logger?.LogInformation("- {Name} by {Author} ({Downloads} downloads) - {Rating:F1}⭐",
                    item.Name, item.Author, item.DownloadCount, item.Rating);
            }

            // Show categories
            _logger?.LogInformation("📂 Browse Categories:");
            var categories = new[] { "Characters", "Stages", "Screenpacks", "Music", "Patches" };
            for (int i = 0; i < categories.Length; i++)
            {
                var count = _workshopItems.Count(item => item.Category == categories[i]);
                _logger?.LogInformation("{Index}. {Category} ({Count} items)", i + 1, categories[i], count);
            }

            // Show user's uploads
            if (_currentUser?.UploadedItems.Any() == true)
            {
                _logger?.LogInformation("📤 Your Uploads:");
                foreach (var item in _currentUser.UploadedItems.Take(3))
                {
                    _logger?.LogInformation("- {Name} ({Downloads} downloads, {Rating:F1}⭐)",
                        item.Name, item.DownloadCount, item.Rating);
                }
            }

            _logger?.LogInformation("Commands:");
            _logger?.LogInformation("- browse [category] - Browse items in category");
            _logger?.LogInformation("- download [id] - Download workshop item");
            _logger?.LogInformation("- upload - Upload your creation");
            _logger?.LogInformation("- search [term] - Search workshop items");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing workshop");
        }
    }

    private async Task ShowCommunityHubAsync()
    {
        try
        {
            _logger?.LogInformation("Showing community hub");

            _logger?.LogInformation("👥 MUGEN Community Hub");
            _logger?.LogInformation("Connect with fellow MUGEN players");

            // Show online friends
            _logger?.LogInformation("🟢 Online Friends:");
            var onlineFriends = new[]
            {
                new { Name = "StreetFighterFan", Status = "In Match", Game = "Ranked" },
                new { Name = "CharacterCreator", Status = "In Workshop", Game = "Creating" },
                new { Name = "ComboMaster", Status = "Training", Game = "Practice" }
            };

            foreach (var friend in onlineFriends)
            {
                _logger?.LogInformation("- {Name}: {Status} ({Game})", friend.Name, friend.Status, friend.Game);
            }

            // Show community events
            _logger?.LogInformation("📅 Community Events:");
            _logger?.LogInformation("- Tournament: Weekly Championship (Starts in 2 hours)");
            _logger?.LogInformation("- Workshop: Character Creation Contest (Ends in 3 days)");
            _logger?.LogInformation("- Stream: Pro Player Tournament (Live now)");

            // Show leaderboard
            _logger?.LogInformation("🥇 Global Leaderboard:");
            var leaderboard = new[]
            {
                new { Rank = 1, Name = "MUGEN Champion", Rating = 2850, Wins = 247 },
                new { Rank = 2, Name = "Combo Legend", Rating = 2720, Wins = 198 },
                new { Rank = 3, Name = "Technique Master", Rating = 2680, Wins = 176 }
            };

            foreach (var entry in leaderboard)
            {
                _logger?.LogInformation("#{Rank} {Name} - {Rating} RP ({Wins} wins)",
                    entry.Rank, entry.Name, entry.Rating, entry.Wins);
            }

            _logger?.LogInformation("Commands:");
            _logger?.LogInformation("- invite [friend] - Invite friend to match");
            _logger?.LogInformation("- join [event] - Join community event");
            _logger?.LogInformation("- message [player] - Send private message");
            _logger?.LogInformation("- report [issue] - Report player or bug");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing community hub");
        }
    }

    private async Task ShowProfileAsync()
    {
        _logger?.LogInformation("Showing player profile");

        if (_currentUser == null)
        {
            _logger?.LogWarning("No user profile loaded. Please log in.");
            return;
        }

        ShowProfileHeader();
        ShowProfileStatistics();
        ShowProfileAchievements();
        ShowProfileWorkshopStats();
        ShowProfileCharacters();
    }

    private void ShowProfileHeader()
    {
        _logger?.LogInformation("👤 Player Profile: {Name}", _currentUser!.DisplayName);
    }

    private void ShowProfileStatistics()
    {
        _logger?.LogInformation("📊 Statistics:");
        _logger?.LogInformation("- Rating: {Rating} RP (Rank: {Rank})",
            _currentUser!.Rating, GetRankFromRating(_currentUser.Rating));
        _logger?.LogInformation("- Total Matches: {Matches}", _currentUser.TotalMatches);
        _logger?.LogInformation("- Win Rate: {WinRate:F1}%", _currentUser.GetWinRate());
        _logger?.LogInformation("- Current Streak: {Streak} {Type}",
            Math.Abs(_currentUser.CurrentStreak),
            _currentUser.CurrentStreak > 0 ? "wins" : "losses");
    }

    private void ShowProfileAchievements()
    {
        _logger?.LogInformation("🏆 Achievements:");
        foreach (var achievement in _currentUser!.UnlockedAchievements.Take(5))
        {
            _logger?.LogInformation("- {Achievement}", achievement);
        }
    }

    private void ShowProfileWorkshopStats()
    {
        _logger?.LogInformation("📦 Workshop Stats:");
        _logger?.LogInformation("- Items Uploaded: {Count}", _currentUser!.UploadedItems.Count);
        _logger?.LogInformation("- Total Downloads: {Downloads}",
            _currentUser.UploadedItems.Sum(i => i.DownloadCount));
        _logger?.LogInformation("- Average Rating: {Rating:F1}⭐",
            _currentUser.UploadedItems.Any() ? _currentUser.UploadedItems.Average(i => i.Rating) : 0);
    }

    private void ShowProfileCharacters()
    {
        _logger?.LogInformation("🎮 Favorite Characters:");
        foreach (var character in _currentUser!.FavoriteCharacters.Take(3))
        {
            var winRate = _currentUser.GetCharacterWinRate(character);
            _logger?.LogInformation("- {Character}: {WinRate:F1}% win rate ({Matches} matches)",
                character, winRate, _currentUser.GetCharacterMatches(character));
        }

            _logger?.LogInformation("Commands:");
            _logger?.LogInformation("- edit - Edit profile settings");
            _logger?.LogInformation("- friends - Manage friend list");
            _logger?.LogInformation("- history - View match history");
            _logger?.LogInformation("- settings - Network and privacy settings");
        }

    private async Task UpdateLobbiesAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _networkStatus == NetworkStatus.Connected)
            {
                // Simulate fetching lobby data from server
                _availableLobbies.Clear();

                // Add some sample lobbies
                _availableLobbies.AddRange(new[]
                {
                    new LobbyInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Casual Fighters",
                        Host = "PlayerOne",
                        CurrentPlayers = 3,
                        MaxPlayers = 8,
                        GameMode = "Casual",
                        Region = "NA East",
                        Ping = 45
                    },
                    new LobbyInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Ranked Tournament",
                        Host = "ProGamer",
                        CurrentPlayers = 6,
                        MaxPlayers = 8,
                        GameMode = "Ranked",
                        Region = "EU West",
                        Ping = 120
                    },
                    new LobbyInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Character Showdown",
                        Host = "CharMaster",
                        CurrentPlayers = 2,
                        MaxPlayers = 4,
                        GameMode = "Custom",
                        Region = "Asia",
                        Ping = 200
                    }
                });

                await Task.Delay(30000, ct); // Update every 30 seconds
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating lobbies");
        }
    }

    private async Task SyncWorkshopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _networkStatus == NetworkStatus.Connected)
            {
                // Simulate syncing workshop data
                await Task.Delay(60000, ct); // Sync every minute

                // Add some sample workshop items
                if (_workshopItems.Count == 0)
                {
                    _workshopItems.AddRange(new[]
                    {
                        new WorkshopItem
                        {
                            Id = Guid.NewGuid(),
                            Name = "Ultimate Character Pack",
                            Author = "CharCreator",
                            Category = "Characters",
                            Description = "50 hand-crafted characters with unique movesets",
                            DownloadCount = 15420,
                            Rating = 4.8f,
                            IsFeatured = true,
                            FileSize = 250 * 1024 * 1024 // 250MB
                        },
                        new WorkshopItem
                        {
                            Id = Guid.NewGuid(),
                            Name = "Cyberpunk Stage Collection",
                            Author = "StageMaster",
                            Category = "Stages",
                            Description = "10 futuristic stages with dynamic backgrounds",
                            DownloadCount = 8750,
                            Rating = 4.6f,
                            IsFeatured = true,
                            FileSize = 150 * 1024 * 1024 // 150MB
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing workshop");
        }
    }

    private async Task LoadUserProfileAsync(CancellationToken ct)
    {
        try
        {
            // In a real implementation, this would load from secure storage
            // or authenticate with the server

            _currentUser = new UserProfile
            {
                Id = Guid.NewGuid(),
                Username = "MugenPlayer",
                DisplayName = "MUGEN Fighter",
                Rating = 1850,
                TotalMatches = 247,
                TotalWins = 176,
                CurrentStreak = 3,
                FavoriteCharacters = new[] { "Ryu", "Ken", "Guile", "Chun-Li" },
                UnlockedAchievements = new[] { "First Victory", "Combo Master", "Character Collector" },
                UploadedItems = new List<WorkshopItem>(),
                JoinDate = DateTime.UtcNow.AddMonths(-6)
            };

            _logger?.LogInformation("Loaded user profile for {User}", _currentUser.DisplayName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading user profile");
        }
    }

    private static string GetRankFromRating(int rating) =>
        rating switch
        {
            >= 3000 => "Grandmaster",
            >= 2800 => "Master",
            >= 2600 => "Diamond",
            >= 2400 => "Platinum",
            >= 2200 => "Gold",
            >= 2000 => "Silver",
            >= 1800 => "Bronze",
            _ => "Unranked"
        };

        private readonly ILogger? _logger;
        private readonly ITaskRunner? _taskRunner;
        private TcpListener? _listener;
        private readonly List<TcpClient> _peers = new();
        private bool _isRunning;

        public MugenNetworkManager(ILogger? logger, ITaskRunner? taskRunner = null)
        {
            _logger = logger;
            _taskRunner = taskRunner;
        }

        public void StartListener(int port)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                _isRunning = true;

                if (_taskRunner != null)
                {
                    _taskRunner.Run(AcceptPeersAsync(), "MugenAcceptPeers");
                }
                else
                {
                    _ = Task.Run(AcceptPeersAsync);
                }

                _logger?.LogInformation("MUGEN P2P Listener started on port {Port}", port);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start P2P listener");
            }
        }

        private async Task AcceptPeersAsync()
        {
            while (_isRunning && _listener != null)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    lock (_peers)
                    {
                        _peers.Add(client);
                    }

                    if (_taskRunner != null)
                    {
                        _taskRunner.Run(HandlePeerMessagesAsync(client), $"MugenHandlePeer_{client.Client.RemoteEndPoint}");
                    }
                    else
                    {
                        _ = Task.Run(() => HandlePeerMessagesAsync(client));
                    }
                }
                catch (Exception ex) when (_isRunning)
                {
                    _logger?.LogError(ex, "Error accepting peer connection");
                }
            }
        }

        private async Task HandlePeerMessagesAsync(TcpClient client)
        {
            using var stream = client.GetStream();
            var buffer = new byte[4096];

            try
            {
                while (client.Connected)
                {
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0) break;

                    // Handle MUGEN network protocol messages (sync, input, etc.)
                    var message = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
                    _logger?.LogDebug("Received from peer: {Message}", message);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Peer disconnected: {Message}", ex.Message);
            }
            finally
            {
                lock (_peers)
                {
                    _peers.Remove(client);
                }
                client.Dispose();
            }
        }

        public async Task MaintainConnectionsAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // Send heartbeats, check peer health, etc.
                await Task.Delay(15000, ct);

                lock (_peers)
                {
                    foreach (var peer in _peers.ToList())
                    {
                        if (!peer.Connected)
                        {
                            _peers.Remove(peer);
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            lock (_peers)
            {
                foreach (var peer in _peers) peer.Dispose();
                _peers.Clear();
            }
        }
    }
}

/// <summary>
/// Network connection status.
/// </summary>
public enum NetworkStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error
}

/// <summary>
/// Multiplayer lobby information.
/// </summary>
public class LobbyInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public string GameMode { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int Ping { get; set; }
    public bool HasPassword { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Workshop item information.
/// </summary>
public class WorkshopItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DownloadCount { get; set; }
    public float Rating { get; set; }
    public bool IsFeatured { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadDate { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// User profile information.
/// </summary>
public class UserProfile
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public int TotalMatches { get; set; }
    public int TotalWins { get; set; }
    public int CurrentStreak { get; set; }
    public IEnumerable<string> FavoriteCharacters { get; set; } = Array.Empty<string>();
    public IEnumerable<string> UnlockedAchievements { get; set; } = Array.Empty<string>();
    public List<WorkshopItem> UploadedItems { get; set; } = new();
    public DateTime JoinDate { get; set; }

    public float GetWinRate() => TotalMatches > 0 ? (TotalWins / (float)TotalMatches) * 100 : 0;

    public int GetCharacterMatches(string character) => TotalMatches / FavoriteCharacters.Count(); // Simplified

    public float GetCharacterWinRate(string character) => GetWinRate() + Random.Shared.Next(-10, 11); // Simplified
}
