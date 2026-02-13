using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Infrastructure.Cloud;

namespace SaveState.Infrastructure.Ai;

/// <summary>
/// Image analysis service using Google Cloud Vision API.
/// PHASE 1: Core Services - Screenshot Scanning Implementation.
/// </summary>
public class ImageAnalysisService : IImageAnalysisService
{
    private readonly GoogleCloudService _googleCloudService;
    private readonly ILogger<ImageAnalysisService> _logger;

    // Common gaming-related labels to filter for relevance
    private static readonly HashSet<string> GamingRelevantLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Video game", "Action game", "Fighting game", "Role-playing game", "RPG",
        "Adventure game", "Puzzle game", "Strategy game", "Simulation game",
        "Sports game", "Racing game", "Shooter game", "First-person shooter", "FPS",
        "Platform game", "Platformer", "Arcade game", "Horror game",
        "Multiplayer", "Online game", "PC game", "Console game",
        "Character", "Player", "Combat", "Battle", "Quest", "Level",
        "Fantasy", "Science fiction", "Sci-fi", "Medieval", "Futuristic",
        "Anime", "Cartoon", "Animation", "3D graphics", "2D graphics",
        "Pixel art", "Retro", "Indie game"
    };

    // Labels to filter out (too generic)
    private static readonly HashSet<string> GenericLabelsBlocklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Software", "Rectangle", "Font", "Product", "Screenshot",
        "Computer", "Technology", "Text", "Symbol", "Logo",
        "Electric blue", "Multimedia", "Graphics", "Event"
    };

    public ImageAnalysisService(
        GoogleCloudService googleCloudService,
        ILogger<ImageAnalysisService> logger)
    {
        _googleCloudService = googleCloudService ?? throw new ArgumentNullException(nameof(googleCloudService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<ImageAnalysisResult>> AnalyzeImageAsync(
        string imagePath,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return Result.Failure<ImageAnalysisResult>("Image path cannot be empty", ErrorType.Validation);
        }

        if (!File.Exists(imagePath))
        {
            return Result.Failure<ImageAnalysisResult>($"Image file not found: {imagePath}", ErrorType.NotFound);
        }

        // Convert local path to file URI for the API
        var fileUri = new Uri(imagePath).AbsoluteUri;
        return await AnalyzeImageFromUriAsync(fileUri, ct);
    }

    /// <inheritdoc />
    public async Task<Result<ImageAnalysisResult>> AnalyzeImageFromUriAsync(
        string imageUri,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(imageUri))
        {
            return Result.Failure<ImageAnalysisResult>("Image URI cannot be empty", ErrorType.Validation);
        }

#if DEBUG
            _logger.LogDebug("Analyzing image from URI: {ImageUri}", imageUri);
#endif

        try
        {
            var result = await _googleCloudService.AnalyzeImageAsync(imageUri, ct);

            if (!result.IsSuccess)
            {
                return Result.Failure<ImageAnalysisResult>(result.Error ?? "Image analysis failed", ErrorType.External);
            }

            var cloudResult = result.Value;

            // Filter labels to remove generic ones
            var filteredLabels = cloudResult.Labels
                .Where(label => !GenericLabelsBlocklist.Contains(label))
                .ToList();

            // Calculate confidence based on label scores (if available) or use default
            var confidence = filteredLabels.Count > 0 ? 0.8f : 0.5f;

            var analysisResult = new ImageAnalysisResult(
                Labels: filteredLabels,
                DetectedText: cloudResult.DetectedText,
                Objects: cloudResult.Objects.ToList(),
                Confidence: confidence);

            _logger.LogInformation(
                "Image analysis completed: {LabelCount} labels, {TextLength} chars text, {ObjectCount} objects",
                filteredLabels.Count,
                cloudResult.DetectedText.Length,
                cloudResult.Objects.Length);

            return Result.Success(analysisResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze image: {ImageUri}", imageUri);
            return Result.Failure<ImageAnalysisResult>($"Image analysis failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> GetSuggestedTagsAsync(
        string imagePath,
        int maxTags = 10,
        CancellationToken ct = default)
    {
        var analysisResult = await AnalyzeImageAsync(imagePath, ct);

        if (!analysisResult.IsSuccess)
        {
            return Result.Failure<IReadOnlyList<string>>(analysisResult.Error ?? "Analysis failed");
        }

        var analysis = analysisResult.Value;

        // Prioritize gaming-relevant labels
        var suggestedTags = new List<string>();

        // First, add gaming-relevant labels
        foreach (var label in analysis.Labels)
        {
            if (GamingRelevantLabels.Contains(label))
            {
                suggestedTags.Add(label);
            }
        }

        // Then add objects that might be relevant
        foreach (var obj in analysis.Objects)
        {
            if (!suggestedTags.Contains(obj, StringComparer.OrdinalIgnoreCase))
            {
                suggestedTags.Add(obj);
            }
        }

        // Add remaining labels that aren't blocklisted
        foreach (var label in analysis.Labels)
        {
            if (!suggestedTags.Contains(label, StringComparer.OrdinalIgnoreCase) &&
                !GenericLabelsBlocklist.Contains(label))
            {
                suggestedTags.Add(label);
            }
        }

        // Limit to max tags
        var finalTags = suggestedTags.Take(maxTags).ToList();

#if DEBUG
        _logger.LogDebug("Generated {TagCount} suggested tags from image analysis", finalTags.Count);
#endif

        return Result.Success<IReadOnlyList<string>>(finalTags);
    }
}
