using System.Text;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Infrastructure.Mugen.ScreenpackEditor;

/// <summary>
/// Implementation of screenpack editing service for MUGEN.
/// </summary>
public class ScreenpackEditorService : IScreenpackEditorService
{
    private readonly ILogger<ScreenpackEditorService> _logger;

    public ScreenpackEditorService(ILogger<ScreenpackEditorService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ScreenpackCreationResult>> CreateScreenpackAsync(
        ScreenpackCreationRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating screenpack: {Name}", request.Name);

            var safeName = SanitizeFileName(request.Name);
            var screenpackDir = Path.Combine("data", safeName);
            Directory.CreateDirectory(screenpackDir);

            var generatedFiles = new List<string>();

            // Create system.def file
            var systemDefPath = Path.Combine(screenpackDir, "system.def");
            await GenerateSystemDefAsync(systemDefPath, request);
            generatedFiles.Add(systemDefPath);

            // Create motif configuration
            var motifPath = Path.Combine(screenpackDir, "motif.cfg");
            await GenerateMotifConfigAsync(motifPath, request);
            generatedFiles.Add(motifPath);

            // Create font configuration
            var fontPath = Path.Combine(screenpackDir, "font.def");
            await GenerateFontConfigAsync(fontPath, request.InitialTheme);
            generatedFiles.Add(fontPath);

            // Create background configuration if animated
            if (request.IncludeAnimatedBackground)
            {
                var bgPath = Path.Combine(screenpackDir, "background.def");
                await GenerateBackgroundConfigAsync(bgPath, request.InitialTheme);
                generatedFiles.Add(bgPath);
            }

            _logger.LogInformation("Screenpack {Name} created with {Count} files", 
                request.Name, generatedFiles.Count);

            return Result<ScreenpackCreationResult>.Success(new ScreenpackCreationResult(
                request.Name,
                screenpackDir,
                generatedFiles,
                true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create screenpack: {Name}", request.Name);
            return Result<ScreenpackCreationResult>.Failure(
                $"Screenpack creation failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ScreenpackData>> LoadScreenpackAsync(
        string screenpackPath, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Loading screenpack: {Path}", screenpackPath);

            if (!Directory.Exists(screenpackPath))
            {
                return Result<ScreenpackData>.Failure(
                    $"Screenpack directory not found: {screenpackPath}",
                    ErrorType.NotFound);
            }

            var systemDefPath = Path.Combine(screenpackPath, "system.def");
            if (!File.Exists(systemDefPath))
            {
                return Result<ScreenpackData>.Failure(
                    $"system.def not found in screenpack: {screenpackPath}",
                    ErrorType.NotFound);
            }

            // Parse system.def file
            var config = await ParseSystemDefAsync(systemDefPath);

            var data = new ScreenpackData(
                config.GetValueOrDefault("Name", Path.GetFileName(screenpackPath)),
                screenpackPath,
                config.GetValueOrDefault("Author", "Unknown"),
                config.GetValueOrDefault("Version", "1.0"),
                new ScreenpackResolution(1280, 720, true, false),
                await LoadThemeAsync(screenpackPath),
                await LoadFontsAsync(screenpackPath),
                await LoadLayoutAsync(screenpackPath),
                await LoadEffectsAsync(screenpackPath),
                Directory.GetFiles(screenpackPath, "*.*", SearchOption.AllDirectories).ToList(),
                File.GetLastWriteTime(systemDefPath));

            return Result<ScreenpackData>.Success(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load screenpack: {Path}", screenpackPath);
            return Result<ScreenpackData>.Failure(
                $"Failed to load screenpack: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateScreenpackThemeAsync(
        string screenpackPath, 
        ScreenpackTheme theme, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating screenpack theme: {Path}", screenpackPath);

            var systemDefPath = Path.Combine(screenpackPath, "system.def");
            if (!File.Exists(systemDefPath))
            {
                return Result.Failure(
                    $"system.def not found: {systemDefPath}",
                    ErrorType.NotFound);
            }

            // Read existing content
            var content = await File.ReadAllTextAsync(systemDefPath, ct);

            // Update colors in the content
            content = UpdateColorInContent(content, "menu.item.active.font.color", theme.SelectionColor);
            content = UpdateColorInContent(content, "menu.item.font.color", theme.TextColor);
            content = UpdateColorInContent(content, "title.bg.color", theme.PrimaryColor);

            await File.WriteAllTextAsync(systemDefPath, content, ct);

            _logger.LogInformation("Screenpack theme updated: {ThemeName}", theme.Name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update screenpack theme: {Path}", screenpackPath);
            return Result.Failure(
                $"Theme update failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateFontsAsync(
        string screenpackPath, 
        FontConfiguration fonts, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating screenpack fonts: {Path}", screenpackPath);

            var fontPath = Path.Combine(screenpackPath, "font.def");
            
            var content = new StringBuilder();
            content.AppendLine($"; Font Configuration");
            content.AppendLine($"; Generated by SaveState Screenpack Editor");
            content.AppendLine();
            content.AppendLine("[Menu Font]");
            content.AppendLine($"name = {fonts.MenuFontName}");
            content.AppendLine($"size = {fonts.MenuFontSize}");
            content.AppendLine();
            content.AppendLine("[Title Font]");
            content.AppendLine($"name = {fonts.TitleFontName}");
            content.AppendLine($"size = {fonts.TitleFontSize}");
            content.AppendLine();
            content.AppendLine("[Message Font]");
            content.AppendLine($"name = {fonts.MessageFontName}");
            content.AppendLine($"size = {fonts.MessageFontSize}");

            await File.WriteAllTextAsync(fontPath, content.ToString(), ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update fonts: {Path}", screenpackPath);
            return Result.Failure(
                $"Font update failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateMenuLayoutAsync(
        string screenpackPath, 
        MenuLayout layout, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating menu layout: {Path}", screenpackPath);

            var motifPath = Path.Combine(screenpackPath, "motif.cfg");
            
            var content = new StringBuilder();
            content.AppendLine($"; Menu Layout Configuration");
            content.AppendLine();
            content.AppendLine("[Menu Position]");
            content.AppendLine($"x = {layout.MenuX}");
            content.AppendLine($"y = {layout.MenuY}");
            content.AppendLine($"spacing = {layout.MenuItemSpacing}");
            content.AppendLine($"alignment = {layout.Alignment}");
            content.AppendLine();
            content.AppendLine("[Title Position]");
            content.AppendLine($"x = {layout.TitleX}");
            content.AppendLine($"y = {layout.TitleY}");
            content.AppendLine();
            content.AppendLine("[Logo]");
            content.AppendLine($"show = {(layout.ShowLogo ? 1 : 0)}");
            content.AppendLine($"x = {layout.LogoX}");
            content.AppendLine($"y = {layout.LogoY}");
            content.AppendLine($"scale = {layout.LogoScalePercent / 100.0}");

            await File.WriteAllTextAsync(motifPath, content.ToString(), ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update menu layout: {Path}", screenpackPath);
            return Result.Failure(
                $"Layout update failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateBackgroundEffectsAsync(
        string screenpackPath, 
        BackgroundEffects effects, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating background effects: {Path}", screenpackPath);

            var bgPath = Path.Combine(screenpackPath, "background.def");
            
            var content = new StringBuilder();
            content.AppendLine($"; Background Effects Configuration");
            content.AppendLine();
            content.AppendLine("[Background]");
            content.AppendLine($"type = {(effects.EnableAnimation ? "animated" : "static")}");
            content.AppendLine();
            content.AppendLine("[Particles]");
            content.AppendLine($"enabled = {(effects.EnableParticles ? 1 : 0)}");
            content.AppendLine($"type = {effects.ParticleType}");
            content.AppendLine($"count = {effects.ParticleCount}");
            content.AppendLine();
            content.AppendLine("[Audio]");
            content.AppendLine($"music = {(effects.EnableMusic ? 1 : 0)}");
            content.AppendLine($"musicVolume = {effects.MusicVolume}");
            content.AppendLine($"sfx = {(effects.EnableSoundEffects ? 1 : 0)}");
            content.AppendLine($"sfxVolume = {effects.SFXVolume}");

            await File.WriteAllTextAsync(bgPath, content.ToString(), ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update background effects: {Path}", screenpackPath);
            return Result.Failure(
                $"Effects update failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> ExportScreenpackAsync(
        string screenpackPath, 
        string outputDirectory, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Exporting screenpack: {Path}", screenpackPath);

            if (!Directory.Exists(screenpackPath))
            {
                return Result<string>.Failure(
                    $"Screenpack not found: {screenpackPath}",
                    ErrorType.NotFound);
            }

            Directory.CreateDirectory(outputDirectory);
            var screenpackName = Path.GetFileName(screenpackPath);
            var exportPath = Path.Combine(outputDirectory, $"{screenpackName}.zip");

            // Create zip archive
            System.IO.Compression.ZipFile.CreateFromDirectory(screenpackPath, exportPath);

            _logger.LogInformation("Screenpack exported to: {ExportPath}", exportPath);
            return Result<string>.Success(exportPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export screenpack: {Path}", screenpackPath);
            return Result<string>.Failure(
                $"Export failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ScreenpackPreview>> GeneratePreviewAsync(
        string screenpackPath, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating preview for: {Path}", screenpackPath);

            // Load screenpack data
            var loadResult = await LoadScreenpackAsync(screenpackPath, ct);
            if (loadResult.IsFailure)
            {
                return Result<ScreenpackPreview>.Failure(loadResult.Error!, loadResult.ErrorType);
            }

            var data = loadResult.Value;

            // For now, return a placeholder preview
            // In a real implementation, this would render the screenpack to an image
            var preview = new ScreenpackPreview(
                data.Name,
                Array.Empty<byte>(), // Placeholder for actual image data
                data.Theme.Name,
                data.Resolution.Width,
                data.Resolution.Height);

            return Result<ScreenpackPreview>.Success(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate preview: {Path}", screenpackPath);
            return Result<ScreenpackPreview>.Failure(
                $"Preview generation failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ScreenpackTemplate>>> GetTemplatesAsync(
        CancellationToken ct = default)
    {
        var templates = new List<ScreenpackTemplate>
        {
            new ScreenpackTemplate(
                "default",
                "Default MUGEN",
                "Classic MUGEN style screenpack",
                "Elecbyte",
                "templates/default/preview.png",
                new ScreenpackResolution(1280, 720, true, false),
                false,
                new[] { "classic", "official" }.ToList()),

            new ScreenpackTemplate(
                "modern-dark",
                "Modern Dark",
                "Sleek dark theme with animated background",
                "Community",
                "templates/modern-dark/preview.png",
                new ScreenpackResolution(1920, 1080, true, true),
                true,
                new[] { "modern", "dark", "animated" }.ToList()),

            new ScreenpackTemplate(
                "retro-arcade",
                "Retro Arcade",
                "80s arcade style with neon colors",
                "Community",
                "templates/retro-arcade/preview.png",
                new ScreenpackResolution(1280, 720, false, false),
                true,
                new[] { "retro", "arcade", "neon" }.ToList()),

            new ScreenpackTemplate(
                "minimal",
                "Minimal Clean",
                "Clean minimal design with focus on readability",
                "Community",
                "templates/minimal/preview.png",
                new ScreenpackResolution(1920, 1080, true, false),
                false,
                new[] { "minimal", "clean", "simple" }.ToList())
        };

        return Task.FromResult(Result<IReadOnlyList<ScreenpackTemplate>>.Success(templates));
    }

    /// <inheritdoc />
    public async Task<Result<ValidationResult>> ValidateScreenpackAsync(
        string screenpackPath, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Validating screenpack: {Path}", screenpackPath);

            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();

            if (!Directory.Exists(screenpackPath))
            {
                errors.Add(new ValidationError("DIR_NOT_FOUND", $"Directory not found: {screenpackPath}"));
                return Result<ValidationResult>.Success(new ValidationResult(false, errors, warnings, new List<string>()));
            }

            // Check required files
            var requiredFiles = new[] { "system.def" };
            foreach (var file in requiredFiles)
            {
                var filePath = Path.Combine(screenpackPath, file);
                if (!File.Exists(filePath))
                {
                    errors.Add(new ValidationError("MISSING_FILE", $"Required file missing: {file}"));
                }
            }

            // Check for recommended files
            var recommendedFiles = new[] { "font.def", "motif.cfg" };
            foreach (var file in recommendedFiles)
            {
                var filePath = Path.Combine(screenpackPath, file);
                if (!File.Exists(filePath))
                {
                    warnings.Add(new ValidationWarning("MISSING_RECOMMENDED", $"Recommended file missing: {file}"));
                }
            }

            var isValid = errors.Count == 0;

            return Result<ValidationResult>.Success(new ValidationResult(
                isValid,
                errors,
                warnings,
                new List<string>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed for: {Path}", screenpackPath);
            return Result<ValidationResult>.Failure(
                $"Validation failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    #region Private Helper Methods

    private string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private async Task GenerateSystemDefAsync(string path, ScreenpackCreationRequest request)
    {
        var content = new StringBuilder();
        content.AppendLine($"; {request.Name} Screenpack");
        content.AppendLine($"; Author: {request.Author}");
        content.AppendLine($"; {request.Description}");
        content.AppendLine();
        content.AppendLine("[Info]");
        content.AppendLine($"name = \"{request.Name}\"");
        content.AppendLine($"author = \"{request.Author}\"");
        content.AppendLine($"version = 1.0");
        content.AppendLine();
        content.AppendLine("[Video]");
        content.AppendLine($"width = {request.Resolution.Width}");
        content.AppendLine($"height = {request.Resolution.Height}");
        content.AppendLine("depth = 32");
        content.AppendLine();
        content.AppendLine("[Menu]");
        content.AppendLine("rows = 10");
        content.AppendLine("columns = 2");
        content.AppendLine("pos = 640, 480");
        content.AppendLine("item.font = 1,0,0");
        content.AppendLine("item.active.font = 1,1,0");

        await File.WriteAllTextAsync(path, content.ToString());
    }

    private async Task GenerateMotifConfigAsync(string path, ScreenpackCreationRequest request)
    {
        var content = new StringBuilder();
        content.AppendLine($"; Motif Configuration");
        content.AppendLine();
        content.AppendLine("[Menu]");
        content.AppendLine("x = 320");
        content.AppendLine("y = 240");
        content.AppendLine("spacing = 20");
        content.AppendLine("alignment = center");

        await File.WriteAllTextAsync(path, content.ToString());
    }

    private async Task GenerateFontConfigAsync(string path, ScreenpackTheme theme)
    {
        var content = new StringBuilder();
        content.AppendLine($"; Font Configuration");
        content.AppendLine();
        content.AppendLine("[Font 1]");
        content.AppendLine("name = default");
        content.AppendLine("size = 24");
        content.AppendLine($"color = {theme.TextColor.R},{theme.TextColor.G},{theme.TextColor.B}");

        await File.WriteAllTextAsync(path, content.ToString());
    }

    private async Task GenerateBackgroundConfigAsync(string path, ScreenpackTheme theme)
    {
        var content = new StringBuilder();
        content.AppendLine($"; Background Configuration");
        content.AppendLine();
        content.AppendLine("[Background 1]");
        content.AppendLine("type = static");
        content.AppendLine("spriteno = 0,0");
        content.AppendLine("layerno = 0");
        content.AppendLine("start = 0,0");
        content.AppendLine("delta = 1,1");

        await File.WriteAllTextAsync(path, content.ToString());
    }

    private async Task<Dictionary<string, string>> ParseSystemDefAsync(string path)
    {
        var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        if (!File.Exists(path))
            return config;

        var lines = await File.ReadAllLinesAsync(path);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";"))
                continue;

            var parts = trimmed.Split('=', 2);
            if (parts.Length == 2)
            {
                config[parts[0].Trim()] = parts[1].Trim().Trim('"');
            }
        }

        return config;
    }

    private async Task<ScreenpackTheme> LoadThemeAsync(string screenpackPath)
    {
        // Default theme
        return new ScreenpackTheme(
            "Default",
            new MugenColor(0, 128, 255),
            new MugenColor(255, 255, 255),
            new MugenColor(255, 200, 0),
            new MugenColor(0, 0, 0),
            new MugenColor(255, 255, 255),
            new MugenColor(255, 255, 0),
            BackgroundType.Static,
            null,
            SaveState.Core.Mugen.Services.AnimationType.None,
            TransitionType.Fade);
    }

    private async Task<FontConfiguration> LoadFontsAsync(string screenpackPath)
    {
        return new FontConfiguration(
            "default",
            24,
            "default",
            48,
            "default",
            16,
            FontStyle.Regular,
            true,
            true,
            new MugenColor(0, 0, 0),
            2,
            2);
    }

    private async Task<MenuLayout> LoadLayoutAsync(string screenpackPath)
    {
        return new MenuLayout(
            320, 240, 20,
            MenuAlignment.Center,
            640, 100,
            true,
            640, 50,
            true,
            640, 300,
            100);
    }

    private async Task<BackgroundEffects> LoadEffectsAsync(string screenpackPath)
    {
        return new BackgroundEffects(
            false,
            ParticleType.None,
            0,
            new MugenColor(255, 255, 255),
            false,
            null,
            false,
            1.0f,
            1.0f,
            false,
            null,
            60);
    }

    private string UpdateColorInContent(string content, string key, MugenColor color)
    {
        var lines = content.Split('\n');
        var newLines = new List<string>();
        var inTargetSection = false;

        foreach (var line in lines)
        {
            if (line.Trim().StartsWith($"{key} ="))
            {
                newLines.Add($"{key} = {color.R},{color.G},{color.B}");
                inTargetSection = true;
            }
            else
            {
                newLines.Add(line);
            }
        }

        if (!inTargetSection)
        {
            // Add the key if it doesn't exist
            newLines.Add($"{key} = {color.R},{color.G},{color.B}");
        }

        return string.Join('\n', newLines);
    }

    #endregion
}
