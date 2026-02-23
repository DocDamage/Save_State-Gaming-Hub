using SaveState.Core.Esports.Models;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.RgbSync.Models;
using SaveState.Core.Theme.Models;
using SaveState.Core.WebBrowser.Models;

namespace SaveState.IntegrationTests.Helpers;

/// <summary>
/// Seeds test data for integration tests.
/// </summary>
public static class TestDataSeeder
{
    /// <summary>
    /// Creates a sample tournament for testing.
    /// </summary>
    public static CreateTournamentRequest CreateSampleTournamentRequest(string name = "Test Tournament")
    {
        return new CreateTournamentRequest(
            Name: name,
            Description: "A test tournament for integration testing",
            Game: new GameInfo
            {
                GameId = Guid.NewGuid(),
                Name = "Street Fighter 6",
                Platform = "PC"
            },
            Format: TournamentFormat.SingleElimination,
            StartDate: DateTime.UtcNow.AddDays(1),
            RegistrationDeadline: DateTime.UtcNow.AddHours(12),
            MaxParticipants: 16,
            Rules: new TournamentRules
            {
                BestOf = 3,
                TimeLimit = TimeSpan.FromMinutes(5),
                AllowCharacterSwitch = true,
                RandomStageSelect = false
            },
            PrizePool: new PrizePool
            {
                TotalAmount = 1000m,
                Currency = "USD",
                Distribution = new List<PrizeDistribution>
                {
                    new() { Place = 1, Amount = 500, Percentage = 50 },
                    new() { Place = 2, Amount = 300, Percentage = 30 },
                    new() { Place = 3, Amount = 200, Percentage = 20 }
                }
            }
        );
    }

    /// <summary>
    /// Creates a list of sample participants for testing.
    /// </summary>
    public static List<RegisterParticipantRequest> CreateSampleParticipants(int count)
    {
        var participants = new List<RegisterParticipantRequest>();
        for (int i = 1; i <= count; i++)
        {
            participants.Add(new RegisterParticipantRequest(
                UserId: $"user_{i}",
                DisplayName: $"Player {i}",
                Seed: i
            ));
        }
        return participants;
    }

    /// <summary>
    /// Creates a sample mobile device for testing.
    /// </summary>
    public static MobileDevice CreateSampleMobileDevice(string name = "Test Device")
    {
        return new MobileDevice
        {
            Id = Guid.NewGuid(),
            DeviceName = name,
            DeviceType = "iOS",
            DeviceModel = "iPhone 15 Pro",
            OsVersion = "17.0",
            AppVersion = "1.0.0",
            PairedAt = DateTime.UtcNow,
            IsConnected = false,
            Status = ConnectionStatus.Disconnected,
            Permissions = new List<string> { "remote_control", "notifications" }
        };
    }

    /// <summary>
    /// Creates a sample pairing request for testing.
    /// </summary>
    public static PairingRequest CreateSamplePairingRequest()
    {
        return new PairingRequest
        {
            Id = Guid.NewGuid(),
            PairingCode = GeneratePairingCode(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };
    }

    /// <summary>
    /// Creates a sample RGB device for testing.
    /// </summary>
    public static RgbDevice CreateSampleRgbDevice(string name = "Test Keyboard", RgbDeviceType type = RgbDeviceType.Keyboard)
    {
        return new RgbDevice
        {
            Id = Guid.NewGuid(),
            Name = name,
            Vendor = "Test Vendor",
            Type = type,
            LedCount = type switch
            {
                RgbDeviceType.Keyboard => 104,
                RgbDeviceType.Mouse => 4,
                RgbDeviceType.Headset => 2,
                _ => 1
            },
            Leds = Enumerable.Range(0, type switch
            {
                RgbDeviceType.Keyboard => 104,
                RgbDeviceType.Mouse => 4,
                RgbDeviceType.Headset => 2,
                _ => 1
            }).Select(i => new RgbLed { Index = i, Color = new RgbColor(255, 255, 255), Brightness = 1.0f }).ToList(),
            IsConnected = true,
            SupportsDirectMode = true,
            ProviderId = "test_provider"
        };
    }

    /// <summary>
    /// Creates a sample RGB effect for testing.
    /// </summary>
    public static RgbEffect CreateSampleRgbEffect(string name = "Rainbow Wave")
    {
        return new RgbEffect
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = RgbEffectType.Rainbow,
            Colors = new List<RgbColor> { RgbColor.Red, RgbColor.Green, RgbColor.Blue },
            Speed = 1.0f,
            Brightness = 1.0f,
            Direction = RgbDirection.Forward,
            Parameters = new Dictionary<string, object>(),
            IsEnabled = true
        };
    }

    /// <summary>
    /// Creates a sample RGB profile for testing.
    /// </summary>
    public static RgbProfile CreateSampleRgbProfile(string name = "Gaming Profile")
    {
        return new RgbProfile
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsDefault = false,
            DeviceEffects = new Dictionary<Guid, RgbEffect>(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a sample theme definition for testing.
    /// </summary>
    public static ThemeDefinition CreateSampleTheme(string name = "Test Theme", bool isDark = false)
    {
        return new ThemeDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsBuiltIn = false,
            IsDark = isDark,
            Colors = new ThemeColors
            {
                Primary = "#6750A4",
                OnPrimary = "#FFFFFF",
                PrimaryContainer = "#EADDFF",
                OnPrimaryContainer = "#21005D",
                Secondary = "#625B71",
                OnSecondary = "#FFFFFF",
                SecondaryContainer = "#E8DEF8",
                OnSecondaryContainer = "#1D192B",
                Tertiary = "#7D5260",
                OnTertiary = "#FFFFFF",
                TertiaryContainer = "#FFD8E4",
                OnTertiaryContainer = "#31111D",
                Error = "#B3261E",
                OnError = "#FFFFFF",
                ErrorContainer = "#F9DEDC",
                OnErrorContainer = "#410E0B",
                Background = isDark ? "#1C1B1F" : "#FFFBFE",
                OnBackground = isDark ? "#E6E1E5" : "#1C1B1F",
                Surface = isDark ? "#1C1B1F" : "#FFFBFE",
                OnSurface = isDark ? "#E6E1E5" : "#1C1B1F"
            },
            Typography = new ThemeTypography
            {
                DisplayFont = "Inter",
                BodyFont = "Inter",
                MonoFont = "JetBrains Mono",
                BaseFontSize = 14
            },
            Effects = new ThemeEffects
            {
                GlassBlur = 20,
                GlassOpacity = 0.2,
                ShadowOpacity = 0.3,
                BorderRadius = 12,
                BorderWidth = 1,
                UseAnimations = true,
                AnimationSpeed = 1.0
            },
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a sample browser tab for testing.
    /// </summary>
    public static BrowserTab CreateSampleBrowserTab(string url = "https://example.com", string title = "Example")
    {
        return new BrowserTab
        {
            Id = Guid.NewGuid(),
            Title = title,
            Url = url,
            State = BrowserTabState.Loaded,
            CanGoBack = false,
            CanGoForward = false,
            IsLoading = false,
            LoadingProgress = 100,
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            IsMuted = false,
            IsPinned = false,
            IsIncognito = false,
            Zoom = ZoomLevel.Default
        };
    }

    /// <summary>
    /// Creates sample browser settings for testing.
    /// </summary>
    public static BrowserSettings CreateSampleBrowserSettings()
    {
        return new BrowserSettings
        {
            HomePage = "https://www.google.com",
            SearchEngine = "https://www.google.com/search?q=",
            EnableJavaScript = true,
            EnablePlugins = true,
            EnableWebSecurity = true,
            ClearDataOnExit = false,
            DoNotTrack = true,
            BlockPopups = true
        };
    }

    /// <summary>
    /// Generates a random 6-digit pairing code.
    /// </summary>
    private static string GeneratePairingCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}
