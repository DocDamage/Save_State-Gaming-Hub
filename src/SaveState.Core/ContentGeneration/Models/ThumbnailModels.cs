namespace SaveState.Core.ContentGeneration.Models;

/// <summary>
/// Available styles for AI-generated thumbnails.
/// </summary>
public enum ThumbnailStyle
{
    Cinematic,
    Minimalist,
    Retro,
    Vibrant,
    Dark,
    Cyberpunk,
    Fantasy
}

/// <summary>
/// Request to generate a thumbnail for a game.
/// </summary>
public record ThumbnailRequest
{
    public required IReadOnlyList<Screenshot> Screenshots { get; init; }
    public required string GameTitle { get; init; }
    public required ThumbnailStyle Style { get; init; }
    public required string? CustomPrompt { get; init; }
    public required ThumbnailDimensions Dimensions { get; init; }
}

/// <summary>
/// Dimensions for generated thumbnails.
/// </summary>
public record ThumbnailDimensions
{
    public required int Width { get; init; }
    public required int Height { get; init; }

    public static ThumbnailDimensions YouTube => new() { Width = 1280, Height = 720 };
    public static ThumbnailDimensions Twitch => new() { Width = 1200, Height = 480 };
    public static ThumbnailDimensions Steam => new() { Width = 460, Height = 215 };
}

/// <summary>
/// An AI-generated image result.
/// </summary>
public record GeneratedImage
{
    public required string ImageData { get; init; } // Base64
    public required string Format { get; init; }
    public required ThumbnailDimensions Dimensions { get; init; }
    public required string? PromptUsed { get; init; }
    public required DateTime GeneratedAt { get; init; }
}

/// <summary>
/// A screenshot captured from gameplay.
/// </summary>
public record Screenshot
{
    public required string ImageData { get; init; }
    public required DateTime CapturedAt { get; init; }
    public required string? GameContext { get; init; }
}
