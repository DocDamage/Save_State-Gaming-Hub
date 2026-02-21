namespace SaveState.Core.GameLibrary.Models.Recommendations;

/// <summary>
/// Represents different times of day for contextual recommendations.
/// </summary>
public enum TimeOfDay
{
    /// <summary>Morning hours (6AM - 12PM)</summary>
    Morning,
    /// <summary>Afternoon hours (12PM - 5PM)</summary>
    Afternoon,
    /// <summary>Evening hours (5PM - 10PM)</summary>
    Evening,
    /// <summary>Night hours (10PM - 6AM)</summary>
    Night
}

/// <summary>
/// Represents the player's current mood for mood-based recommendations.
/// </summary>
public enum Mood
{
    /// <summary>Relaxed and casual gaming</summary>
    Relaxed,
    /// <summary>Competitive and focused gaming</summary>
    Competitive,
    /// <summary>Adventure-seeking and exploratory</summary>
    Adventurous,
    /// <summary>Nostalgic for classic or retro games</summary>
    Nostalgic,
    /// <summary>Social and multiplayer oriented</summary>
    Social
}

/// <summary>
/// Reasons for game recommendations to provide transparency to users.
/// </summary>
public enum RecommendationReason
{
    /// <summary>Similar to games played recently</summary>
    SimilarToRecent,
    /// <summary>Matches user's genre preferences</summary>
    GenrePreference,
    /// <summary>Appropriate for current time of day</summary>
    TimeAppropriate,
    /// <summary>Matches user's current mood</summary>
    MoodMatch,
    /// <summary>Friends are currently playing</summary>
    FriendPlaying,
    /// <summary>Currently trending in the community</summary>
    Trending,
    /// <summary>Hidden gem with high ratings but low visibility</summary>
    HiddenGem,
    /// <summary>Suggestion to complete or continue</summary>
    CompletionSuggestion,
    /// <summary>Game in user's backlog</summary>
    Backlog,
    /// <summary>New release matching preferences</summary>
    NewRelease
}

/// <summary>
/// Contextual information used to generate personalized game recommendations.
/// </summary>
public record RecommendationContext
{
    /// <summary>
    /// The current time of day.
    /// </summary>
    public required TimeOfDay TimeOfDay { get; init; }

    /// <summary>
    /// The current day of the week.
    /// </summary>
    public required DayOfWeek DayOfWeek { get; init; }

    /// <summary>
    /// Amount of time the player has available to play.
    /// </summary>
    public required TimeSpan AvailableTime { get; init; }

    /// <summary>
    /// The player's current mood (null if not specified).
    /// </summary>
    public required Mood? CurrentMood { get; init; }

    /// <summary>
    /// List of game IDs recently played by the user.
    /// </summary>
    public required IReadOnlyList<Guid> RecentlyPlayed { get; init; }

    /// <summary>
    /// List of genres the user prefers.
    /// </summary>
    public required IReadOnlyList<string> PreferredGenres { get; init; }

    /// <summary>
    /// List of platforms the user prefers.
    /// </summary>
    public required IReadOnlyList<string> PreferredPlatforms { get; init; }

    /// <summary>
    /// Number of players (1 = solo, 2+ = multiplayer).
    /// </summary>
    public required int PlayerCount { get; init; }
}

/// <summary>
/// Represents a game recommendation with scoring and explanation.
/// </summary>
public record GameRecommendation
{
    /// <summary>
    /// The unique identifier of the recommended game.
    /// </summary>
    public required Guid GameId { get; init; }

    /// <summary>
    /// The title of the recommended game.
    /// </summary>
    public required string GameTitle { get; init; }

    /// <summary>
    /// The recommendation score (0.0 to 1.0).
    /// </summary>
    public required float Score { get; init; }

    /// <summary>
    /// The primary reason for this recommendation.
    /// </summary>
    public required RecommendationReason Reason { get; init; }

    /// <summary>
    /// List of factors that contributed to this recommendation.
    /// </summary>
    public required IReadOnlyList<string> Factors { get; init; }

    /// <summary>
    /// URL to the game's cover image (null if not available).
    /// </summary>
    public required string? CoverImageUrl { get; init; }

    /// <summary>
    /// Estimated playtime for a typical session.
    /// </summary>
    public required TimeSpan? EstimatedPlaytime { get; init; }

    /// <summary>
    /// Confidence level in this recommendation (0.0 to 1.0).
    /// </summary>
    public required float Confidence { get; init; }
}

/// <summary>
/// Context for determining what to play next after finishing a game.
/// </summary>
public record PlayNextContext
{
    /// <summary>
    /// Amount of time available for the next gaming session.
    /// </summary>
    public required TimeSpan AvailableTime { get; init; }

    /// <summary>
    /// The player's current mood (null if not specified).
    /// </summary>
    public required Mood? CurrentMood { get; init; }

    /// <summary>
    /// List of game IDs just finished by the user.
    /// </summary>
    public required IReadOnlyList<Guid> JustFinished { get; init; }

    /// <summary>
    /// The current date and time.
    /// </summary>
    public required DateTime CurrentTime { get; init; }
}

/// <summary>
/// Context for social-based recommendations.
/// </summary>
public record SocialRecommendationContext
{
    /// <summary>
    /// List of friend usernames.
    /// </summary>
    public required IReadOnlyList<string> FriendUsernames { get; init; }

    /// <summary>
    /// List of game IDs that friends are currently playing.
    /// </summary>
    public required IReadOnlyList<Guid> FriendsCurrentlyPlaying { get; init; }

    /// <summary>
    /// List of game IDs recommended by friends.
    /// </summary>
    public required IReadOnlyList<Guid> FriendsRecommendations { get; init; }
}
