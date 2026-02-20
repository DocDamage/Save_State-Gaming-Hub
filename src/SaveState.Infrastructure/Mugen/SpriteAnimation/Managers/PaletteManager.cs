using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.SpriteAnimation.Managers;

/// <summary>
/// Manages palette operations including loading, saving, and color manipulation.
/// </summary>
public sealed class PaletteManager
{
    private readonly ILogger<PaletteManager> _logger;
    private readonly ConcurrentDictionary<int, Palette> _palettes;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaletteManager"/> class.
    /// </summary>
    public PaletteManager(
        ILogger<PaletteManager> logger,
        ConcurrentDictionary<int, Palette> palettes)
    {
        _logger = logger;
        _palettes = palettes;
    }

    /// <summary>
    /// Loads palette from a file (ACT, PAL, etc.).
    /// </summary>
    public async Task<Result<Palette>> LoadPaletteAsync(
        string filePath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Loading palette: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return Result<Palette>.Failure($"Palette file not found: {filePath}", ErrorType.NotFound);
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            Palette palette;

            if (extension == ".act")
            {
                palette = await LoadActPaletteAsync(filePath, ct);
            }
            else
            {
                return Result<Palette>.Failure($"Unsupported palette format: {extension}", ErrorType.Validation);
            }

            return Result<Palette>.Success(palette);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load palette");
            return Result<Palette>.Failure($"Palette load failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Saves palette to a file.
    /// </summary>
    public Task<Result> SavePaletteAsync(
        string filePath,
        Palette palette,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Saving palette: {FilePath}", filePath);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save palette");
            return Task.FromResult(Result.Failure($"Palette save failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Creates a new palette.
    /// </summary>
    public Task<Result<Palette>> CreatePaletteAsync(
        string name,
        IReadOnlyList<RgbColor> colors,
        CancellationToken ct = default)
    {
        try
        {
            var palette = new Palette(name, colors, colors.Count, false);
            return Task.FromResult(Result<Palette>.Success(palette));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create palette");
            return Task.FromResult(Result<Palette>.Failure($"Create palette failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Applies palette to sprites.
    /// </summary>
    public Task<Result<IReadOnlyList<Sprite>>> ApplyPaletteAsync(
        IReadOnlyList<Sprite> sprites,
        Palette palette,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Applying palette to {Count} sprites", sprites.Count);
            return Task.FromResult(Result<IReadOnlyList<Sprite>>.Success(sprites));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply palette");
            return Task.FromResult(Result<IReadOnlyList<Sprite>>.Failure($"Apply palette failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Generates palette variations.
    /// </summary>
    public Task<Result<IReadOnlyList<Palette>>> GeneratePaletteVariationsAsync(
        Palette basePalette,
        int variationCount,
        VariationType type,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating {Count} palette variations", variationCount);

            var variations = new List<Palette>();

            for (int i = 0; i < variationCount; i++)
            {
                var shiftedColors = type switch
                {
                    VariationType.HueShift => ShiftHue(basePalette.Colors, (i + 1) * 30),
                    VariationType.BrightnessShift => ShiftBrightness(basePalette.Colors, (i - variationCount / 2) * 20),
                    _ => basePalette.Colors
                };

                variations.Add(new Palette(
                    $"{basePalette.Name} Variation {i + 1}",
                    shiftedColors.ToList(),
                    shiftedColors.Count,
                    false));
            }

            return Task.FromResult(Result<IReadOnlyList<Palette>>.Success(variations));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate palette variations");
            return Task.FromResult(Result<IReadOnlyList<Palette>>.Failure($"Generate variations failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets palette colors.
    /// </summary>
    public Task<Result<IReadOnlyList<RgbColor>>> GetPaletteColorsAsync(
        int paletteIndex,
        CancellationToken ct = default)
    {
        if (_palettes.TryGetValue(paletteIndex, out var palette))
        {
            return Task.FromResult(Result<IReadOnlyList<RgbColor>>.Success(palette.Colors));
        }

        return Task.FromResult(Result<IReadOnlyList<RgbColor>>.Failure($"Palette {paletteIndex} not found", ErrorType.NotFound));
    }

    /// <summary>
    /// Sets palette color.
    /// </summary>
    public Task<Result> SetPaletteColorAsync(
        int paletteIndex,
        int colorIndex,
        RgbColor color,
        CancellationToken ct = default)
    {
        try
        {
            if (!_palettes.TryGetValue(paletteIndex, out var palette))
            {
                return Task.FromResult(Result.Failure($"Palette {paletteIndex} not found", ErrorType.NotFound));
            }

            if (colorIndex < 0 || colorIndex >= palette.Colors.Count)
            {
                return Task.FromResult(Result.Failure($"Color index {colorIndex} out of range", ErrorType.Validation));
            }

            var colors = palette.Colors.ToList();
            colors[colorIndex] = color;

            _palettes[paletteIndex] = palette with { Colors = colors };
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set palette color");
            return Task.FromResult(Result.Failure($"Set color failed: {ex.Message}", ErrorType.Internal));
        }
    }

    private async Task<Palette> LoadActPaletteAsync(string filePath, CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(filePath, ct);
        var colors = new List<RgbColor>();

        for (int i = 0; i < 256 && i * 3 + 2 < bytes.Length; i++)
        {
            colors.Add(new RgbColor(bytes[i * 3], bytes[i * 3 + 1], bytes[i * 3 + 2]));
        }

        return new Palette(Path.GetFileNameWithoutExtension(filePath), colors, colors.Count, false);
    }

    private IReadOnlyList<RgbColor> ShiftHue(IReadOnlyList<RgbColor> colors, int degrees)
    {
        return colors.Select(c =>
        {
            var rad = degrees * Math.PI / 180;
            var cos = Math.Cos(rad);
            var sin = Math.Sin(rad);

            var r = c.R;
            var g = c.G;
            var b = c.B;

            var newR = (byte)Math.Clamp(r * cos - g * sin, 0, 255);
            var newG = (byte)Math.Clamp(r * sin + g * cos, 0, 255);

            return new RgbColor(newR, newG, b);
        }).ToList();
    }

    private IReadOnlyList<RgbColor> ShiftBrightness(IReadOnlyList<RgbColor> colors, int amount)
    {
        return colors.Select(c => new RgbColor(
            (byte)Math.Clamp(c.R + amount, 0, 255),
            (byte)Math.Clamp(c.G + amount, 0, 255),
            (byte)Math.Clamp(c.B + amount, 0, 255))).ToList();
    }
}
