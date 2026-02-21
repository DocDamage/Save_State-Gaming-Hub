namespace SaveState.Core.Analytics.Models.GamerProfile;

/// <summary>
/// Represents the different gamer archetypes based on playstyle preferences.
/// </summary>
public enum GamerArchetype
{
    /// <summary>Focuses on 100% achievements and all collectibles.</summary>
    Completionist,

    /// <summary>Enjoys open world exploration and discovery.</summary>
    Explorer,

    /// <summary>Competitive player focused on PvP, leaderboards, and ranked play.</summary>
    Competitor,

    /// <summary>Prefers narrative-driven games with strong stories.</summary>
    StorySeeker,

    /// <summary>Enjoys turn-based and tactical gameplay.</summary>
    Strategist,

    /// <summary>Focuses on time-optimized play and speedrunning.</summary>
    Speedrunner,

    /// <summary>Prefers multiplayer and co-op experiences.</summary>
    Socialite,

    /// <summary>Focuses on library completion and game collection.</summary>
    Collector,

    /// <summary>Prefers short sessions and low-stress gaming.</summary>
    Casual,

    /// <summary>Enjoys difficult games and long gaming sessions.</summary>
    Hardcore
}

/// <summary>
/// Represents a complete gaming DNA profile for a user.
/// </summary>
public record GamerDnaProfile
{
    /// <summary>The user's unique identifier.</summary>
    public required Guid UserId { get; init; }

    /// <summary>The primary gamer archetype for this user.</summary>
    public required GamerArchetype PrimaryArchetype { get; init; }

    /// <summary>Scores for all archetypes (0.0 - 1.0).</summary>
    public required IReadOnlyDictionary<GamerArchetype, float> ArchetypeScores { get; init; }

    /// <summary>List of genre preferences ordered by preference score.</summary>
    public required IReadOnlyList<GenrePreference> GenrePreferences { get; init; }

    /// <summary>List of platform preferences.</summary>
    public required IReadOnlyList<PlatformPreference> PlatformPreferences { get; init; }

    /// <summary>Detailed playstyle metrics.</summary>
    public required PlaystyleMetrics Playstyle { get; init; }

    /// <summary>When this profile was generated.</summary>
    public required DateTime GeneratedAt { get; init; }

    /// <summary>When this profile was last updated.</summary>
    public required DateTime? LastUpdated { get; init; }
}

/// <summary>
/// Represents a user's preference for a specific game genre.
/// </summary>
public record GenrePreference
{
    /// <summary>The genre name (e.g., "RPG", "FPS", "Strategy").</summary>
    public required string Genre { get; init; }

    /// <summary>Preference score from 0.0 to 1.0.</summary>
    public required float PreferenceScore { get; init; }

    /// <summary>Total hours played in this genre.</summary>
    public required int HoursPlayed { get; init; }

    /// <summary>Number of games played in this genre.</summary>
    public required int GamesPlayed { get; init; }

    /// <summary>Trend direction indicating rising, stable, or declining interest.</summary>
    public required TrendDirection Trend { get; init; }
}

/// <summary>
/// Represents a user's preference for a specific gaming platform.
/// </summary>
public record PlatformPreference
{
    /// <summary>The platform name (e.g., "Steam", "Epic", "PlayStation").</summary>
    public required string Platform { get; init; }

    /// <summary>Preference score from 0.0 to 1.0.</summary>
    public required float PreferenceScore { get; init; }

    /// <summary>Total hours played on this platform.</summary>
    public required int HoursPlayed { get; init; }

    /// <summary>Number of games owned on this platform.</summary>
    public required int GamesOwned { get; init; }
}

/// <summary>
/// Time of day categories for playtime analysis.
/// </summary>
public enum TimeOfDay
{
    /// <summary>6 AM - 12 PM</summary>
    Morning,

    /// <summary>12 PM - 5 PM</summary>
    Afternoon,

    /// <summary>5 PM - 10 PM</summary>
    Evening,

    /// <summary>10 PM - 6 AM</summary>
    Night
}

/// <summary>
/// Comprehensive playstyle metrics for a gamer.
/// </summary>
public record PlaystyleMetrics
{
    /// <summary>Average length of a gaming session.</summary>
    public required TimeSpan AverageSessionLength { get; init; }

    /// <summary>Average time to complete a game.</summary>
    public required TimeSpan AverageTimeToComplete { get; init; }

    /// <summary>Percentage of games completed (0.0 - 1.0).</summary>
    public required float CompletionRate { get; init; }

    /// <summary>Score indicating achievement hunting behavior (0.0 - 1.0).</summary>
    public required float AchievementHunterScore { get; init; }

    /// <summary>Score indicating tendency to replay games (0.0 - 1.0).</summary>
    public required float ReplayabilityScore { get; init; }

    /// <summary>Day of the week with most playtime.</summary>
    public required DayOfWeek MostActiveDay { get; init; }

    /// <summary>Time of day with most playtime.</summary>
    public required TimeOfDay MostActiveTime { get; init; }

    /// <summary>Total number of games owned.</summary>
    public required int TotalGamesOwned { get; init; }

    /// <summary>Total number of games completed.</summary>
    public required int TotalGamesCompleted { get; init; }

    /// <summary>Total achievements unlocked across all games.</summary>
    public required int TotalAchievementsUnlocked { get; init; }

    /// <summary>Total playtime in hours.</summary>
    public required float TotalPlaytimeHours { get; init; }
}

/// <summary>
/// Indicates the trend direction of a preference over time.
/// </summary>
public enum TrendDirection
{
    /// <summary>Increasing preference.</summary>
    Rising,

    /// <summary>Consistent preference.</summary>
    Stable,

    /// <summary>Decreasing preference.</summary>
    Declining
}

/// <summary>
/// Represents a snapshot of a user's gaming DNA evolution at a specific point in time.
/// </summary>
public record DnaEvolutionSnapshot
{
    /// <summary>When this snapshot was taken.</summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>The dominant archetype at this time.</summary>
    public required GamerArchetype DominantArchetype { get; init; }

    /// <summary>Top genres and their scores at this time.</summary>
    public required IReadOnlyDictionary<string, float> TopGenres { get; init; }
}

/// <summary>
/// Represents a quiz question for determining gamer type.
/// </summary>
public record GamerTypeQuizQuestion
{
    /// <summary>The question text.</summary>
    public required string Question { get; init; }

    /// <summary>Available answer options.</summary>
    public required IReadOnlyList<GamerTypeQuizAnswer> Answers { get; init; }
}

/// <summary>
/// Represents an answer option for the gamer type quiz.
/// </summary>
public record GamerTypeQuizAnswer
{
    /// <summary>The answer text.</summary>
    public required string Text { get; init; }

    /// <summary>The archetype this answer contributes to.</summary>
    public required GamerArchetype Archetype { get; init; }

    /// <summary>Weight of this answer (higher = stronger contribution).</summary>
    public required int Weight { get; init; }
}

/// <summary>
/// Result of the gamer type quiz.
/// </summary>
public record GamerTypeQuizResult
{
    /// <summary>The primary archetype determined by the quiz.</summary>
    public required GamerArchetype PrimaryArchetype { get; init; }

    /// <summary>Scores for all archetypes based on answers.</summary>
    public required IReadOnlyDictionary<GamerArchetype, int> ArchetypeScores { get; init; }

    /// <summary>Description of the primary archetype.</summary>
    public required string Description { get; init; }

    /// <summary>Recommended games for this archetype.</summary>
    public required IReadOnlyList<string> RecommendedGenres { get; init; }
}

/// <summary>
/// Represents a shareable profile card for social sharing.
/// </summary>
public record ShareableProfileCard
{
    /// <summary>The user's display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>The primary archetype to highlight.</summary>
    public required GamerArchetype PrimaryArchetype { get; init; }

    /// <summary>Top 3 genre preferences.</summary>
    public required IReadOnlyList<string> TopGenres { get; init; }

    /// <summary>Key statistics for display.</summary>
    public required IReadOnlyDictionary<string, string> KeyStats { get; init; }

    /// <summary>Card theme/color scheme.</summary>
    public required string Theme { get; init; }

    /// <summary>Unique share code.</summary>
    public required string ShareCode { get; init; }
}

/// <summary>
/// Visual theme options for profile cards.
/// </summary>
public enum ProfileCardTheme
{
    /// <summary>Dark theme with neon accents.</summary>
    Cyberpunk,

    /// <summary>Clean minimal design.</summary>
    Minimal,

    /// <summary>Retro pixel art style.</summary>
    Retro,

    /// <summary>Arcade-inspired bright colors.</summary>
    Arcade,

    /// <summary>Professional esports style.</summary>
    Esports
}

/// <summary>
/// Extension methods for GamerArchetype.
/// </summary>
public static class GamerArchetypeExtensions
{
    /// <summary>
    /// Gets a human-readable name for the archetype.
    /// </summary>
    public static string GetDisplayName(this GamerArchetype archetype) => archetype switch
    {
        GamerArchetype.Completionist => "The Completionist",
        GamerArchetype.Explorer => "The Explorer",
        GamerArchetype.Competitor => "The Competitor",
        GamerArchetype.StorySeeker => "The Story Seeker",
        GamerArchetype.Strategist => "The Strategist",
        GamerArchetype.Speedrunner => "The Speedrunner",
        GamerArchetype.Socialite => "The Socialite",
        GamerArchetype.Collector => "The Collector",
        GamerArchetype.Casual => "The Casual",
        GamerArchetype.Hardcore => "The Hardcore",
        _ => archetype.ToString()
    };

    /// <summary>
    /// Gets a description for the archetype.
    /// </summary>
    public static string GetDescription(this GamerArchetype archetype) => archetype switch
    {
        GamerArchetype.Completionist => "You strive for 100% completion in every game. No achievement goes unlocked, no collectible uncollected.",
        GamerArchetype.Explorer => "The journey matters more than the destination. You seek hidden secrets and uncharted territories.",
        GamerArchetype.Competitor => "Victory is everything. You thrive in competitive environments and always aim for the top of the leaderboard.",
        GamerArchetype.StorySeeker => "Every game is a new story to experience. You value narrative depth and character development.",
        GamerArchetype.Strategist => "You prefer to think before you act. Tactical depth and strategic planning are your strengths.",
        GamerArchetype.Speedrunner => "Time is your opponent. You push games to their limits, finding the fastest routes to victory.",
        GamerArchetype.Socialite => "Games are better with friends. You value multiplayer experiences and shared adventures.",
        GamerArchetype.Collector => "Your library is your treasure. You take pride in owning and experiencing diverse games.",
        GamerArchetype.Casual => "Gaming is your relaxation. You prefer low-stress experiences that fit your schedule.",
        GamerArchetype.Hardcore => "You embrace the challenge. Difficult games and long sessions are where you shine.",
        _ => "A unique gaming personality."
    };

    /// <summary>
    /// Gets recommended genres for this archetype.
    /// </summary>
    public static IReadOnlyList<string> GetRecommendedGenres(this GamerArchetype archetype) => archetype switch
    {
        GamerArchetype.Completionist => new[] { "Open World", "Metroidvania", "Platformer" },
        GamerArchetype.Explorer => new[] { "Open World", "Adventure", "Walking Simulator" },
        GamerArchetype.Competitor => new[] { "FPS", "MOBA", "Fighting", "Battle Royale" },
        GamerArchetype.StorySeeker => new[] { "RPG", "Visual Novel", "Adventure", "Interactive Drama" },
        GamerArchetype.Strategist => new[] { "Strategy", "Tactical", "Turn-Based", "RTS" },
        GamerArchetype.Speedrunner => new[] { "Platformer", "Action", "Roguelike" },
        GamerArchetype.Socialite => new[] { "Co-op", "MMO", "Party", "Multiplayer" },
        GamerArchetype.Collector => new[] { "RPG", "Indie", "Simulation" },
        GamerArchetype.Casual => new[] { "Puzzle", "Casual", "Mobile Port", "Relaxing" },
        GamerArchetype.Hardcore => new[] { "Souls-like", "Roguelike", "Permadeath", "Hard" },
        _ => new[] { "Various" }
    };

    /// <summary>
    /// Gets an emoji/icon representation for the archetype.
    /// </summary>
    public static string GetIcon(this GamerArchetype archetype) => archetype switch
    {
        GamerArchetype.Completionist => "🏆",
        GamerArchetype.Explorer => "🗺️",
        GamerArchetype.Competitor => "⚔️",
        GamerArchetype.StorySeeker => "📖",
        GamerArchetype.Strategist => "♟️",
        GamerArchetype.Speedrunner => "⏱️",
        GamerArchetype.Socialite => "👥",
        GamerArchetype.Collector => "🎮",
        GamerArchetype.Casual => "☕",
        GamerArchetype.Hardcore => "💀",
        _ => "🎯"
    };

    /// <summary>
    /// Gets the primary color associated with this archetype.
    /// </summary>
    public static string GetPrimaryColor(this GamerArchetype archetype) => archetype switch
    {
        GamerArchetype.Completionist => "#FFD700", // Gold
        GamerArchetype.Explorer => "#228B22",     // Forest Green
        GamerArchetype.Competitor => "#DC143C",   // Crimson
        GamerArchetype.StorySeeker => "#8A2BE2",  // Blue Violet
        GamerArchetype.Strategist => "#4169E1",   // Royal Blue
        GamerArchetype.Speedrunner => "#FF4500",  // Orange Red
        GamerArchetype.Socialite => "#FF69B4",    // Hot Pink
        GamerArchetype.Collector => "#9370DB",    // Medium Purple
        GamerArchetype.Casual => "#87CEEB",       // Sky Blue
        GamerArchetype.Hardcore => "#2F4F4F",     // Dark Slate Gray
        _ => "#808080"
    };
}

/// <summary>
/// Extension methods for TrendDirection.
/// </summary>
public static class TrendDirectionExtensions
{
    /// <summary>
    /// Gets an icon representing the trend direction.
    /// </summary>
    public static string GetIcon(this TrendDirection trend) => trend switch
    {
        TrendDirection.Rising => "📈",
        TrendDirection.Stable => "➡️",
        TrendDirection.Declining => "📉",
        _ => "➖"
    };

    /// <summary>
    /// Gets a color representing the trend direction.
    /// </summary>
    public static string GetColor(this TrendDirection trend) => trend switch
    {
        TrendDirection.Rising => "#00FF00",
        TrendDirection.Stable => "#FFFF00",
        TrendDirection.Declining => "#FF0000",
        _ => "#808080"
    };
}
