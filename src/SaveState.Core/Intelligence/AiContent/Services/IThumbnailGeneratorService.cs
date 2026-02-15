using SaveState.Core.Common;

namespace SaveState.Core.Intelligence.AiContent.Services;

/// <summary>
/// Service for generating AI-powered thumbnails and artwork for games.
/// Supports DALL-E, Stable Diffusion, and other generative AI providers.
/// </summary>
public interface IThumbnailGeneratorService
{
    /// <summary>
    /// Generates a thumbnail for a game using AI.
    /// </summary>
    /// <param name="request">The generation request parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the generated thumbnail.</returns>
    Task<Result<GeneratedThumbnail>> GenerateThumbnailAsync(
        ThumbnailGenerationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Generates multiple thumbnail variations for A/B testing or selection.
    /// </summary>
    /// <param name="request">The generation request parameters.</param>
    /// <param name="variationCount">Number of variations to generate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing multiple generated thumbnails.</returns>
    Task<Result<IReadOnlyList<GeneratedThumbnail>>> GenerateVariationsAsync(
        ThumbnailGenerationRequest request,
        int variationCount = 3,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a thumbnail based on an existing image (style transfer/variation).
    /// </summary>
    /// <param name="baseImageUrl">URL of the base image.</param>
    /// <param name="request">Additional generation parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the generated thumbnail.</returns>
    Task<Result<GeneratedThumbnail>> GenerateVariationFromImageAsync(
        string baseImageUrl,
        ThumbnailVariationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Upscales an existing thumbnail to higher resolution.
    /// </summary>
    /// <param name="imageUrl">URL of the image to upscale.</param>
    /// <param name="targetResolution">Target resolution.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the upscaled thumbnail.</returns>
    Task<Result<GeneratedThumbnail>> UpscaleAsync(
        string imageUrl,
        ImageResolution targetResolution,
        CancellationToken ct = default);

    /// <summary>
    /// Gets available art styles for thumbnail generation.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of available art styles.</returns>
    Task<Result<IReadOnlyList<ArtStyle>>> GetAvailableStylesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets generation history for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated generation history.</returns>
    Task<Result<PagedGenerationHistory>> GetGenerationHistoryAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a generated thumbnail.
    /// </summary>
    /// <param name="thumbnailId">The thumbnail ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the delete operation.</returns>
    Task<Result> DeleteThumbnailAsync(
        Guid thumbnailId,
        CancellationToken ct = default);
}

/// <summary>
/// Request for thumbnail generation.
/// </summary>
public sealed record ThumbnailGenerationRequest(
    Guid? GameId,
    string? GameTitle,
    string? Description,
    IReadOnlyList<string>? Genres,
    IReadOnlyList<string>? Tags,
    ArtStyle? Style = null,
    ImageResolution? TargetResolution = null,
    ThumbnailAspectRatio AspectRatio = ThumbnailAspectRatio.Square,
    bool IncludeText = false,
    string? CustomPrompt = null,
    GenerationQuality Quality = GenerationQuality.Standard);

/// <summary>
/// Request for thumbnail variation from existing image.
/// </summary>
public sealed record ThumbnailVariationRequest(
    string? Description,
    ArtStyle? Style = null,
    float VariationStrength = 0.5f,
    ImageResolution? TargetResolution = null,
    ThumbnailAspectRatio AspectRatio = ThumbnailAspectRatio.Square);

/// <summary>
/// Generated thumbnail result.
/// </summary>
public sealed record GeneratedThumbnail(
    Guid Id,
    string Url,
    string? LocalPath,
    ImageResolution Resolution,
    ThumbnailAspectRatio AspectRatio,
    ArtStyle? Style,
    GenerationQuality Quality,
    string? PromptUsed,
    DateTime GeneratedAt,
    GenerationMetadata Metadata);

/// <summary>
/// Generation metadata.
/// </summary>
public sealed record GenerationMetadata(
    string Provider,
    TimeSpan GenerationTime,
    int? Seed,
    float? Cost,
    IReadOnlyList<GenerationParameter>? Parameters);

/// <summary>
/// Generation parameter.
/// </summary>
public sealed record GenerationParameter(
    string Name,
    string Value);

/// <summary>
/// Art style for thumbnail generation.
/// </summary>
public sealed record ArtStyle(
    string Id,
    string Name,
    string Description,
    string? PreviewUrl,
    IReadOnlyList<string>? Tags,
    float? CostMultiplier);

/// <summary>
/// Image resolution.
/// </summary>
public sealed record ImageResolution(
    int Width,
    int Height)
{
    public static ImageResolution Small => new(256, 256);
    public static ImageResolution Medium => new(512, 512);
    public static ImageResolution Large => new(1024, 1024);
    public static ImageResolution XLarge => new(2048, 2048);
    public static ImageResolution Hd1080 => new(1920, 1080);
    public static ImageResolution Hd1440 => new(2560, 1440);
    public static ImageResolution Uhd4K => new(3840, 2160);

    public static ImageResolution ForAspectRatio(ThumbnailAspectRatio ratio, int baseSize = 1024)
    {
        return ratio switch
        {
            ThumbnailAspectRatio.Square => new ImageResolution(baseSize, baseSize),
            ThumbnailAspectRatio.Portrait => new ImageResolution((int)(baseSize * 0.75), baseSize),
            ThumbnailAspectRatio.Landscape => new ImageResolution(baseSize, (int)(baseSize * 0.75)),
            ThumbnailAspectRatio.Wide => new ImageResolution((int)(baseSize * 1.5), (int)(baseSize * 0.5)),
            ThumbnailAspectRatio.Ultrawide => new ImageResolution(baseSize * 2, (int)(baseSize * 0.5)),
            _ => new ImageResolution(baseSize, baseSize)
        };
    }
}

/// <summary>
/// Aspect ratio for thumbnails.
/// </summary>
public enum ThumbnailAspectRatio
{
    Square,
    Portrait,
    Landscape,
    Wide,
    Ultrawide
}

/// <summary>
/// Generation quality level.
/// </summary>
public enum GenerationQuality
{
    Draft,
    Standard,
    High,
    Premium
}

/// <summary>
/// Paged generation history.
/// </summary>
public sealed record PagedGenerationHistory(
    IReadOnlyList<GenerationHistoryItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>
/// Single generation history item.
/// </summary>
public sealed record GenerationHistoryItem(
    Guid Id,
    Guid? GameId,
    string? GameTitle,
    string ThumbnailUrl,
    ArtStyle? Style,
    ImageResolution Resolution,
    GenerationQuality Quality,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? ErrorMessage);

/// <summary>
/// Configuration for AI content generation.
/// </summary>
public sealed record AiContentGenerationOptions
{
    public string DefaultProvider { get; set; } = "OpenAI";
    public string? OpenAiApiKey { get; set; }
    public string? StableDiffusionEndpoint { get; set; }
    public int MaxConcurrentGenerations { get; set; } = 3;
    public TimeSpan GenerationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableCaching { get; set; } = true;
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(24);
}
