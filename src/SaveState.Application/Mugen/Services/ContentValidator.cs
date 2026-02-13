using SaveState.Application.Mugen.Models.ContentMarketplace;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Content validation service for marketplace submissions.
/// Ensures content meets quality standards, compatibility requirements, and safety guidelines.
/// </summary>
public class ContentValidator
{
    private readonly ILogger<ContentValidator> _logger;

    public ContentValidator(ILogger<ContentValidator> logger)
    {
        _logger = logger;
    }

    public async Task<Result> ValidateContentAsync(ContentUploadRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Validating content upload: {Name}", request.Name);

            var errors = new List<string>();

            // Basic validation
            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add("Content name is required");

            if (string.IsNullOrWhiteSpace(request.Description))
                errors.Add("Content description is required");

            if (request.Price < 0)
                errors.Add("Price cannot be negative");

            if (!request.ContentFiles.Any())
                errors.Add("At least one content file is required");

            // Category-specific validation
            var categoryErrors = await ValidateCategorySpecificAsync(request, ct);
            errors.AddRange(categoryErrors);

            // Compatibility validation
            var compatibilityErrors = await ValidateCompatibilityAsync(request, ct);
            errors.AddRange(compatibilityErrors);

            // Security validation
            var securityErrors = await ValidateSecurityAsync(request, ct);
            errors.AddRange(securityErrors);

            if (errors.Any())
            {
                var errorMessage = string.Join("; ", errors);
                _logger.LogWarning("Content validation failed: {Errors}", errorMessage);
                return Result.Failure($"Validation failed: {errorMessage}");
            }

            _logger.LogInformation("Content validation passed for: {Name}", request.Name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating content: {Name}", request.Name);
            return Result.Failure($"Validation error: {ex.Message}");
        }
    }

    private async Task<IReadOnlyList<string>> ValidateCategorySpecificAsync(ContentUploadRequest request, CancellationToken ct)
    {
        var errors = new List<string>();

        switch (request.Category)
        {
            case ContentCategory.Characters:
                errors.AddRange(await ValidateCharacterContentAsync(request, ct));
                break;

            case ContentCategory.Stages:
                errors.AddRange(await ValidateStageContentAsync(request, ct));
                break;

            case ContentCategory.Music:
                errors.AddRange(await ValidateMusicContentAsync(request, ct));
                break;

            case ContentCategory.Screenpacks:
                errors.AddRange(await ValidateScreenpackContentAsync(request, ct));
                break;
        }

        return errors;
    }

    private async Task<IReadOnlyList<string>> ValidateCharacterContentAsync(ContentUploadRequest request, CancellationToken ct)
    {
        var errors = new List<string>();

        // Check for required character files
        var hasDefFile = request.ContentFiles.Any(f => f.EndsWith(".def", StringComparison.OrdinalIgnoreCase));
        if (!hasDefFile)
            errors.Add("Character must include a .def file");

        // Check for sprite files
        var hasSprites = request.ContentFiles.Any(f =>
            f.EndsWith(".sff", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".pcx", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
        if (!hasSprites)
            errors.Add("Character must include sprite files (.sff, .pcx, or .png)");

        // Check for command file
        var hasCommands = request.ContentFiles.Any(f => f.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase));
        if (!hasCommands)
            errors.Add("Character should include a .cmd file for move inputs");

        // Validate character name uniqueness (simplified)
        if (request.Tags.Contains("ryu") && !request.Name.Contains("Ryu", StringComparison.OrdinalIgnoreCase))
            errors.Add("Character tagged as 'ryu' should include 'Ryu' in the name");

        return errors;
    }

    private async Task<IReadOnlyList<string>> ValidateStageContentAsync(ContentUploadRequest request, CancellationToken ct)
    {
        var errors = new List<string>();

        // Check for required stage files
        var hasDefFile = request.ContentFiles.Any(f => f.EndsWith(".def", StringComparison.OrdinalIgnoreCase));
        if (!hasDefFile)
            errors.Add("Stage must include a .def file");

        // Check for background files
        var hasBackgrounds = request.ContentFiles.Any(f =>
            f.EndsWith(".pcx", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));
        if (!hasBackgrounds)
            errors.Add("Stage should include background image files");

        // Validate stage dimensions (simplified check)
        if (request.Tags.Contains("large") && !request.Description.Contains("large", StringComparison.OrdinalIgnoreCase))
            errors.Add("Stage tagged as 'large' should mention size in description");

        return errors;
    }

    private async Task<IReadOnlyList<string>> ValidateMusicContentAsync(ContentUploadRequest request, CancellationToken ct)
    {
        var errors = new List<string>();

        // Check for audio files
        var hasAudioFiles = request.ContentFiles.Any(f =>
            f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".flac", StringComparison.OrdinalIgnoreCase));
        if (!hasAudioFiles)
            errors.Add("Music pack must include audio files (.mp3, .wav, .ogg, .flac)");

        // Validate music quality (simplified)
        if (request.Tags.Contains("ost") && request.ContentFiles.Count < 5)
            errors.Add("OST packs should include at least 5 tracks");

        return errors;
    }

    private async Task<IReadOnlyList<string>> ValidateScreenpackContentAsync(ContentUploadRequest request, CancellationToken ct)
    {
        var errors = new List<string>();

        // Check for screen files
        var hasScreenFiles = request.ContentFiles.Any(f =>
            f.Contains("fight", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("select", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("menu", StringComparison.OrdinalIgnoreCase));
        if (!hasScreenFiles)
            errors.Add("Screenpack should include fight, select, and menu screens");

        return errors;
    }

    private async Task<IReadOnlyList<string>> ValidateCompatibilityAsync(ContentUploadRequest request, CancellationToken ct)
    {
        var errors = new List<string>();

        if (!request.CompatibleVersions.Any())
        {
            errors.Add("At least one compatible MUGEN/IKEMEN version must be specified");
            return errors;
        }

        // Validate version format
        foreach (var version in request.CompatibleVersions)
        {
            if (!IsValidVersionString(version))
                errors.Add($"Invalid version format: {version}");
        }

        // Check for reasonable version compatibility
        if (request.CompatibleVersions.All(v => v.Contains("IKEMEN")) &&
            !request.CompatibleVersions.Any(v => v.Contains("MUGEN")))
        {
            errors.Add("Content should be compatible with both MUGEN and IKEMEN when possible");
        }

        return errors;
    }

    private async Task<IReadOnlyList<string>> ValidateSecurityAsync(ContentUploadRequest request, CancellationToken ct)
    {
        var errors = new List<string>();

        // Check for potentially harmful file types
        var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".scr", ".pif", ".com" };
        foreach (var file in request.ContentFiles)
        {
            var extension = Path.GetExtension(file).ToLowerInvariant();
            if (dangerousExtensions.Contains(extension))
            {
                errors.Add($"Dangerous file type not allowed: {extension}");
            }
        }

        // Check file sizes (simplified - would check actual file sizes)
        if (request.ContentFiles.Count > 100)
            errors.Add("Too many files - content should be organized in a reasonable number of files");

        // Validate file names for special characters
        foreach (var file in request.ContentFiles)
        {
            if (file.Contains("..") || file.Contains("\\") || file.Contains("/"))
                errors.Add($"Invalid file path: {file}");
        }

        return errors;
    }

    private bool IsValidVersionString(string version)
    {
        // Basic validation for version strings like "MUGEN 1.0", "IKEMEN GO", etc.
        return !string.IsNullOrWhiteSpace(version) &&
               version.Length >= 3 &&
               version.Length <= 50 &&
               !version.Contains("<") &&
               !version.Contains(">") &&
               !version.Contains("|");
    }
}
