using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Theme.Models;

namespace SaveState.Infrastructure.Theme.Services;

/// <summary>
/// Service for importing and exporting themes in various formats.
/// </summary>
public interface IThemeImportExportService
{
    /// <summary>
    /// Exports a theme to the specified format.
    /// </summary>
    Task<Result<string>> ExportAsync(ThemeDefinition theme, ThemeFormat format, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a theme from the specified format.
    /// </summary>
    Task<Result<ThemeDefinition>> ImportAsync(string data, ThemeFormat format, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a theme to a file.
    /// </summary>
    Task<Result<string>> ExportToFileAsync(ThemeDefinition theme, ThemeFormat format, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a theme from a file.
    /// </summary>
    Task<Result<ThemeDefinition>> ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects the format of theme data.
    /// </summary>
    Result<ThemeFormat> DetectFormat(string data);

    /// <summary>
    /// Exports a theme to ASE (Adobe Swatch Exchange) format.
    /// </summary>
    Task<Result<byte[]>> ExportToAseAsync(ThemeDefinition theme, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a theme to CLR (Color Palette) format.
    /// </summary>
    Task<Result<byte[]>> ExportToClrAsync(ThemeDefinition theme, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets supported export formats.
    /// </summary>
    IReadOnlyList<ThemeFormat> SupportedExportFormats { get; }

    /// <summary>
    /// Gets supported import formats.
    /// </summary>
    IReadOnlyList<ThemeFormat> SupportedImportFormats { get; }
}

/// <summary>
/// Implementation of theme import/export service.
/// </summary>
public sealed class ThemeImportExportService : IThemeImportExportService
{
    private readonly ILogger<ThemeImportExportService> _logger;
    private readonly ITimeProvider _timeProvider;

    public ThemeImportExportService(ILogger<ThemeImportExportService> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public IReadOnlyList<ThemeFormat> SupportedExportFormats => new[]
    {
        ThemeFormat.Json,
        ThemeFormat.Xml,
        ThemeFormat.Ase,
        ThemeFormat.Clr
    };

    public IReadOnlyList<ThemeFormat> SupportedImportFormats => new[]
    {
        ThemeFormat.Json,
        ThemeFormat.Xml
    };

    public ThemeImportExportService(ILogger<ThemeImportExportService> logger) : this(logger, SystemTimeProvider.Instance)
    {
    }

    public Task<Result<string>> ExportAsync(ThemeDefinition theme, ThemeFormat format, CancellationToken cancellationToken = default)
    {
        try
        {
            var export = format switch
            {
                ThemeFormat.Json => ExportToJson(theme),
                ThemeFormat.Xml => ExportToXml(theme),
                _ => throw new NotSupportedException($"Export format {format} is not supported as string")
            };

            return Task.FromResult(Result<string>.Success(export));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export theme to {Format}", format);
            return Task.FromResult(Result<string>.Failure($"Export failed: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<ThemeDefinition>> ImportAsync(string data, ThemeFormat format, CancellationToken cancellationToken = default)
    {
        try
        {
            var theme = format switch
            {
                ThemeFormat.Json => ImportFromJson(data),
                ThemeFormat.Xml => ImportFromXml(data),
                _ => throw new NotSupportedException($"Import format {format} is not supported")
            };

            return Task.FromResult(Result<ThemeDefinition>.Success(theme));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse theme JSON");
            return Task.FromResult(Result<ThemeDefinition>.Failure($"Invalid JSON: {ex.Message}", ErrorType.Validation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import theme from {Format}", format);
            return Task.FromResult(Result<ThemeDefinition>.Failure($"Import failed: {ex.Message}", ErrorType.Internal));
        }
    }

    public async Task<Result<string>> ExportToFileAsync(ThemeDefinition theme, ThemeFormat format, string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (format is ThemeFormat.Ase or ThemeFormat.Clr)
            {
                // Binary formats
                byte[] data = format == ThemeFormat.Ase
                    ? (await ExportToAseAsync(theme, cancellationToken)).Value
                    : (await ExportToClrAsync(theme, cancellationToken)).Value;

                await File.WriteAllBytesAsync(filePath, data, cancellationToken);
            }
            else
            {
                // Text formats
                var result = await ExportAsync(theme, format, cancellationToken);
                if (result.IsFailure)
                    return result.ToResult<string>();

                await File.WriteAllTextAsync(filePath, result.Value!, cancellationToken);
            }

            _logger.LogInformation("Exported theme {ThemeName} to {FilePath}", theme.Name, filePath);
            return Result<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export theme to file");
            return Result<string>.Failure($"Export to file failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<ThemeDefinition>> ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return Result<ThemeDefinition>.Failure($"File not found: {filePath}", ErrorType.NotFound);
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var format = extension switch
            {
                ".json" => ThemeFormat.Json,
                ".xml" => ThemeFormat.Xml,
                ".ase" => ThemeFormat.Ase,
                ".clr" => ThemeFormat.Clr,
                _ => ThemeFormat.Json
            };

            if (format is ThemeFormat.Ase or ThemeFormat.Clr)
            {
                // Binary formats not yet supported for import
                return Result<ThemeDefinition>.Failure($"Import from {format} not yet supported", ErrorType.NotImplemented);
            }

            var data = await File.ReadAllTextAsync(filePath, cancellationToken);
            return await ImportAsync(data, format, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import theme from file");
            return Result<ThemeDefinition>.Failure($"Import from file failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Result<ThemeFormat> DetectFormat(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return Result<ThemeFormat>.Failure("Data is empty", ErrorType.Validation);
        }

        var trimmed = data.Trim();

        // Check for JSON
        if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
            (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
        {
            try
            {
                JsonDocument.Parse(trimmed);
                return Result<ThemeFormat>.Success(ThemeFormat.Json);
            }
            catch
            {
                // Not valid JSON
            }
        }

        // Check for XML
        if (trimmed.StartsWith("<?xml") || trimmed.StartsWith("<Theme"))
        {
            try
            {
                XDocument.Parse(trimmed);
                return Result<ThemeFormat>.Success(ThemeFormat.Xml);
            }
            catch
            {
                // Not valid XML
            }
        }

        return Result<ThemeFormat>.Failure("Could not detect format", ErrorType.Validation);
    }

    public Task<Result<byte[]>> ExportToAseAsync(ThemeDefinition theme, CancellationToken cancellationToken = default)
    {
        try
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.ASCII);

            // ASE file header
            writer.Write(Encoding.ASCII.GetBytes("ASEF")); // Signature
            writer.Write((short)1); // Major version
            writer.Write((short)0); // Minor version
            writer.Write(0); // Number of blocks (placeholder)

            int blockCount = 0;

            // Write color entries
            void WriteColor(string name, string hexColor)
            {
                var argb = HexToArgb(hexColor);
                var r = ((argb >> 16) & 0xFF) / 255.0f;
                var g = ((argb >> 8) & 0xFF) / 255.0f;
                var b = (argb & 0xFF) / 255.0f;

                // Block type: 1 = color entry
                writer.Write((short)1);

                // Block length (calculated later)
                var blockStart = ms.Position;
                writer.Write(0);

                // Color name (null-terminated Unicode)
                var nameBytes = Encoding.BigEndianUnicode.GetBytes(name + '\0');
                writer.Write((short)nameBytes.Length);
                writer.Write(nameBytes);

                // Color model
                writer.Write(Encoding.ASCII.GetBytes("RGB "));

                // Color values
                writer.Write(r);
                writer.Write(g);
                writer.Write(b);

                // Color type (0 = global)
                writer.Write((short)0);

                // Update block length
                var blockEnd = ms.Position;
                ms.Position = blockStart;
                writer.Write((int)(blockEnd - blockStart - 4));
                ms.Position = blockEnd;

                blockCount++;
            }

            WriteColor("Primary", theme.Colors.Primary);
            WriteColor("Secondary", theme.Colors.Secondary);
            WriteColor("Tertiary", theme.Colors.Tertiary);
            WriteColor("Error", theme.Colors.Error);
            WriteColor("Background", theme.Colors.Background);
            WriteColor("Surface", theme.Colors.Surface);
            WriteColor("Outline", theme.Colors.Outline);

            // Update block count
            ms.Position = 6;
            writer.Write(blockCount);

            return Task.FromResult(Result<byte[]>.Success(ms.ToArray()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export to ASE format");
            return Task.FromResult(Result<byte[]>.Failure($"ASE export failed: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<byte[]>> ExportToClrAsync(ThemeDefinition theme, CancellationToken cancellationToken = default)
    {
        try
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Simple CLR format (binary color list)
            // Header
            writer.Write(Encoding.ASCII.GetBytes("CLR\0"));
            writer.Write(1); // Version

            // Color count
            var colors = new[]
            {
                ("Primary", theme.Colors.Primary),
                ("Secondary", theme.Colors.Secondary),
                ("Tertiary", theme.Colors.Tertiary),
                ("Error", theme.Colors.Error),
                ("Background", theme.Colors.Background),
                ("Surface", theme.Colors.Surface),
                ("Outline", theme.Colors.Outline)
            };

            writer.Write(colors.Length);

            foreach (var (name, hex) in colors)
            {
                var argb = HexToArgb(hex);

                // Name length and name
                var nameBytes = Encoding.UTF8.GetBytes(name);
                writer.Write(nameBytes.Length);
                writer.Write(nameBytes);

                // ARGB values
                writer.Write((byte)((argb >> 24) & 0xFF));
                writer.Write((byte)((argb >> 16) & 0xFF));
                writer.Write((byte)((argb >> 8) & 0xFF));
                writer.Write((byte)(argb & 0xFF));
            }

            return Task.FromResult(Result<byte[]>.Success(ms.ToArray()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export to CLR format");
            return Task.FromResult(Result<byte[]>.Failure($"CLR export failed: {ex.Message}", ErrorType.Internal));
        }
    }

    private static string ExportToJson(ThemeDefinition theme)
    {
        return JsonSerializer.Serialize(theme, JsonOptions);
    }

    private static string ExportToXml(ThemeDefinition theme)
    {
        var doc = new XDocument(
            new XElement("Theme",
                new XElement("Id", theme.Id),
                new XElement("Name", theme.Name),
                new XElement("IsBuiltIn", theme.IsBuiltIn),
                new XElement("IsDark", theme.IsDark),
                new XElement("CreatedAt", theme.CreatedAt),
                new XElement("ModifiedAt", theme.ModifiedAt),
                new XElement("Colors",
                    new XElement("Primary", theme.Colors.Primary),
                    new XElement("OnPrimary", theme.Colors.OnPrimary),
                    new XElement("PrimaryContainer", theme.Colors.PrimaryContainer),
                    new XElement("OnPrimaryContainer", theme.Colors.OnPrimaryContainer),
                    new XElement("Secondary", theme.Colors.Secondary),
                    new XElement("OnSecondary", theme.Colors.OnSecondary),
                    new XElement("SecondaryContainer", theme.Colors.SecondaryContainer),
                    new XElement("OnSecondaryContainer", theme.Colors.OnSecondaryContainer),
                    new XElement("Background", theme.Colors.Background),
                    new XElement("OnBackground", theme.Colors.OnBackground),
                    new XElement("Surface", theme.Colors.Surface),
                    new XElement("OnSurface", theme.Colors.OnSurface),
                    new XElement("Error", theme.Colors.Error),
                    new XElement("OnError", theme.Colors.OnError)
                ),
                new XElement("Typography",
                    new XElement("DisplayFont", theme.Typography.DisplayFont),
                    new XElement("BodyFont", theme.Typography.BodyFont),
                    new XElement("MonoFont", theme.Typography.MonoFont),
                    new XElement("BaseFontSize", theme.Typography.BaseFontSize)
                ),
                new XElement("Effects",
                    new XElement("GlassBlur", theme.Effects.GlassBlur),
                    new XElement("GlassOpacity", theme.Effects.GlassOpacity),
                    new XElement("ShadowOpacity", theme.Effects.ShadowOpacity),
                    new XElement("BorderRadius", theme.Effects.BorderRadius),
                    new XElement("BorderWidth", theme.Effects.BorderWidth),
                    new XElement("UseAnimations", theme.Effects.UseAnimations),
                    new XElement("AnimationSpeed", theme.Effects.AnimationSpeed)
                )
            )
        );

        return doc.ToString();
    }

    private ThemeDefinition ImportFromJson(string data)
    {
        var theme = JsonSerializer.Deserialize<ThemeDefinition>(data, JsonOptions);
        if (theme == null)
            throw new JsonException("Failed to deserialize theme");

        // Ensure new ID for imported themes
        theme.Id = Guid.NewGuid();
        theme.IsBuiltIn = false;
        theme.CreatedAt = _timeProvider.UtcNow;
        theme.ModifiedAt = theme.CreatedAt;

        return theme;
    }

    private ThemeDefinition ImportFromXml(string data)
    {
        var doc = XDocument.Parse(data);
        var root = doc.Element("Theme") ?? throw new InvalidOperationException("Invalid XML: missing Theme element");

        var theme = new ThemeDefinition
        {
            Id = Guid.NewGuid(),
            Name = root.Element("Name")?.Value ?? "Imported Theme",
            IsBuiltIn = false,
            IsDark = bool.TryParse(root.Element("IsDark")?.Value, out var isDark) && isDark,
            Colors = new ThemeColors(),
            Typography = new ThemeTypography(),
            Effects = new ThemeEffects(),
            CreatedAt = _timeProvider.UtcNow,
            ModifiedAt = _timeProvider.UtcNow
        };

        // Parse colors
        var colors = root.Element("Colors");
        if (colors != null)
        {
            theme.Colors.Primary = colors.Element("Primary")?.Value ?? theme.Colors.Primary;
            theme.Colors.OnPrimary = colors.Element("OnPrimary")?.Value ?? theme.Colors.OnPrimary;
            theme.Colors.Secondary = colors.Element("Secondary")?.Value ?? theme.Colors.Secondary;
            theme.Colors.Background = colors.Element("Background")?.Value ?? theme.Colors.Background;
            theme.Colors.Surface = colors.Element("Surface")?.Value ?? theme.Colors.Surface;
            theme.Colors.Error = colors.Element("Error")?.Value ?? theme.Colors.Error;
        }

        // Parse typography
        var typography = root.Element("Typography");
        if (typography != null)
        {
            theme.Typography.DisplayFont = typography.Element("DisplayFont")?.Value ?? theme.Typography.DisplayFont;
            theme.Typography.BodyFont = typography.Element("BodyFont")?.Value ?? theme.Typography.BodyFont;
        }

        return theme;
    }

    private static uint HexToArgb(string hex)
    {
        if (hex.StartsWith("#"))
            hex = hex[1..];

        if (hex.Length == 6)
            return 0xFF000000 | uint.Parse(hex, System.Globalization.NumberStyles.HexNumber);

        if (hex.Length == 8)
            return uint.Parse(hex, System.Globalization.NumberStyles.HexNumber);

        return 0xFF000000;
    }
}
