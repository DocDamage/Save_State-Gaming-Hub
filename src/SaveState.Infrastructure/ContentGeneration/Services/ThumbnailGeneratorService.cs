using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.ContentGeneration.Models;
using SaveState.Core.ContentGeneration.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.ContentGeneration.Services;

/// <summary>
/// Implementation of the thumbnail generator service using AI image generation.
/// </summary>
public class ThumbnailGeneratorService : IThumbnailGeneratorService
{
    private readonly IOpenAiImageClient _imageClient;
    private readonly ILogger<ThumbnailGeneratorService> _logger;
    private readonly ITimeProvider _timeProvider;

    public ThumbnailGeneratorService(
        IOpenAiImageClient imageClient,
        ITimeProvider timeProvider,
        ILogger<ThumbnailGeneratorService> logger)
    {
        _imageClient = imageClient;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<GeneratedImage>> GenerateThumbnailAsync(
        ThumbnailRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var promptResult = await GeneratePromptAsync(
                request.GameTitle,
                request.Style,
                new List<string>(),
                ct);

            if (promptResult.IsFailure)
            {
                return Result<GeneratedImage>.Failure(promptResult.Error!, promptResult.ErrorType);
            }

            var prompt = request.CustomPrompt ?? promptResult.Value;
            var enhancedPrompt = EnhancePromptForStyle(prompt, request.Style);

            _logger.LogInformation("Generating thumbnail for {GameTitle} with style {Style}",
                request.GameTitle, request.Style);

            var imageResult = await _imageClient.GenerateImageAsync(
                enhancedPrompt,
                request.Dimensions.Width,
                request.Dimensions.Height,
                ct);

            if (imageResult.IsFailure)
            {
                return Result<GeneratedImage>.Failure(imageResult.Error!, imageResult.ErrorType);
            }

            var generatedImage = new GeneratedImage
            {
                ImageData = imageResult.Value,
                Format = "png",
                Dimensions = request.Dimensions,
                PromptUsed = enhancedPrompt,
                GeneratedAt = _timeProvider.UtcNow
            };

            return Result<GeneratedImage>.Success(generatedImage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for {GameTitle}", request.GameTitle);
            return Result<GeneratedImage>.Failure("Thumbnail generation failed", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<GeneratedImage>>> GenerateThumbnailVariationsAsync(
        ThumbnailRequest request,
        int count = 4,
        CancellationToken ct = default)
    {
        var variations = new List<GeneratedImage>();
        var styles = Enum.GetValues<ThumbnailStyle>();

        for (int i = 0; i < count && i < styles.Length; i++)
        {
            var styleVariation = styles[i];
            var variationRequest = request with { Style = styleVariation };

            var result = await GenerateThumbnailAsync(variationRequest, ct);
            if (result.IsSuccess)
            {
                variations.Add(result.Value);
            }
        }

        return Result<IReadOnlyList<GeneratedImage>>.Success(variations);
    }

    public Task<Result<string>> GeneratePromptAsync(
        string gameTitle,
        ThumbnailStyle style,
        IReadOnlyList<string> gameTags,
        CancellationToken ct = default)
    {
        var basePrompt = $"Eye-catching thumbnail for '{gameTitle}'. ";

        var styleModifier = style switch
        {
            ThumbnailStyle.Cinematic => "Cinematic lighting, dramatic composition, movie poster style, high contrast",
            ThumbnailStyle.Minimalist => "Clean minimal design, bold typography, lots of negative space, modern",
            ThumbnailStyle.Retro => "8-bit pixel art style, retro gaming aesthetic, vibrant limited palette",
            ThumbnailStyle.Vibrant => "Bright saturated colors, energetic, eye-catching, pop art style",
            ThumbnailStyle.Dark => "Dark moody atmosphere, neon accents, cyber aesthetic, dramatic shadows",
            ThumbnailStyle.Cyberpunk => "Futuristic cyberpunk, neon lights, holographic elements, dystopian vibe",
            ThumbnailStyle.Fantasy => "Epic fantasy art, magical elements, dramatic landscape, heroic",
            _ => "Professional game thumbnail"
        };

        var prompt = basePrompt + styleModifier;

        if (gameTags.Any())
        {
            prompt += $". Themes: {string.Join(", ", gameTags)}.";
        }

        return Task.FromResult(Result<string>.Success(prompt));
    }

    private string EnhancePromptForStyle(string prompt, ThumbnailStyle style)
    {
        return style switch
        {
            ThumbnailStyle.Vibrant => $"{prompt}. Optimized for YouTube thumbnail, engaging, clickable",
            ThumbnailStyle.Cinematic => $"{prompt}. Twitch stream thumbnail, gaming focused",
            _ => prompt
        };
    }
}
