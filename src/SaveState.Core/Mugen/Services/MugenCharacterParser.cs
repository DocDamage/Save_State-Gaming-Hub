namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Implementation of MUGEN character definition file parser.
/// Parses .def files to extract character metadata.
/// </summary>
public class MugenCharacterParser : IMugenCharacterParser
{
    /// <summary>
    /// Parses a MUGEN character definition file and extracts metadata.
    /// </summary>
    /// <param name="definitionFilePath">Path to the .def file.</param>
    /// <param name="characterDirectory">Directory containing the character files.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed character metadata.</returns>
    public async Task<CharacterMetadata> ParseCharacterAsync(string definitionFilePath, string characterDirectory, CancellationToken ct = default)
    {
        if (!File.Exists(definitionFilePath))
        {
            throw new FileNotFoundException("Character definition file not found", definitionFilePath);
        }

        var content = await File.ReadAllTextAsync(definitionFilePath, ct).ConfigureAwait(false);

        // Parse the INI-style content
        var sections = ParseIniContent(content);

        // Extract metadata from different sections
        var infoSection = sections.GetValueOrDefault("Info", new Dictionary<string, string>());
        var filesSection = sections.GetValueOrDefault("Files", new Dictionary<string, string>());
        var arcadeSection = sections.GetValueOrDefault("Arcade", new Dictionary<string, string>());

        var metadata = new CharacterMetadata(
            DisplayName: GetValue(infoSection, "displayname"),
            Version: GetValue(infoSection, "version"),
            Author: GetValue(infoSection, "author"),
            CommandFile: GetValue(filesSection, "cmd"),
            ConstantsFile: GetValue(filesSection, "constants"),
            StatesFile: GetValue(filesSection, "st"),
            CommonStatesFile: GetValue(filesSection, "stcommon"),
            Directories: ParseDirectories(filesSection, characterDirectory),
            PaletteInfo: ParsePaletteInfo(filesSection),
            ArcadeInfo: ParseArcadeInfo(arcadeSection),
            FileSize: new FileInfo(definitionFilePath).Length
        );

        return metadata;
    }

    /// <summary>
    /// Validates whether a file is a valid MUGEN character definition file.
    /// </summary>
    /// <param name="filePath">Path to the potential .def file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the file is a valid character definition.</returns>
    public async Task<bool> IsValidCharacterDefinitionAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath) || !filePath.EndsWith(".def", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);

            // Check for required MUGEN character definition sections
            var hasInfoSection = content.Contains("[Info]", StringComparison.OrdinalIgnoreCase);
            var hasFilesSection = content.Contains("[Files]", StringComparison.OrdinalIgnoreCase);

            return hasInfoSection && hasFilesSection;
        }
        catch
        {
            return false;
        }
    }

    private static Dictionary<string, Dictionary<string, string>> ParseIniContent(string content)
    {
        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string>? currentSection = null;

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                // New section
                var sectionName = trimmedLine[1..^1];
                currentSection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                sections[sectionName] = currentSection;
            }
            else if (currentSection != null && trimmedLine.Contains('='))
            {
                // Key-value pair
                var parts = trimmedLine.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    currentSection[key] = value;
                }
            }
        }

        return sections;
    }

    private static string? GetValue(Dictionary<string, string> section, string key)
    {
        return section.TryGetValue(key, out var value) ? value : null;
    }

    private static CharacterDirectories? ParseDirectories(Dictionary<string, string> filesSection, string characterDirectory)
    {
        var spriteDir = GetValue(filesSection, "sprite");
        var soundDir = GetValue(filesSection, "sound");
        var palDir = GetValue(filesSection, "pal");

        if (spriteDir == null && soundDir == null && palDir == null)
        {
            return null;
        }

        return new CharacterDirectories(
            SpriteDirectory: ResolvePath(spriteDir, characterDirectory),
            SoundDirectory: ResolvePath(soundDir, characterDirectory),
            PaletteDirectory: ResolvePath(palDir, characterDirectory)
        );
    }

    private static PaletteInfo? ParsePaletteInfo(Dictionary<string, string> filesSection)
    {
        var palFile = GetValue(filesSection, "pal");

        // Default to single palette if no palette file specified
        return palFile != null ? new PaletteInfo(1, palFile) : null;
    }

    private static ArcadeInfo? ParseArcadeInfo(Dictionary<string, string> arcadeSection)
    {
        var introStoryboard = GetValue(arcadeSection, "intro.storyboard");
        var endingStoryboard = GetValue(arcadeSection, "ending.storyboard");

        if (introStoryboard == null && endingStoryboard == null)
        {
            return null;
        }

        return new ArcadeInfo(
            IntroStoryboard: int.TryParse(introStoryboard, out var intro) ? intro : 0,
            EndingStoryboard: int.TryParse(endingStoryboard, out var ending) ? ending : 0
        );
    }

    private static string? ResolvePath(string? relativePath, string baseDirectory)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return null;
        }

        // If it's an absolute path, return as-is
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        // Otherwise, resolve relative to character directory
        return Path.Combine(baseDirectory, relativePath);
    }
}
