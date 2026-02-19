using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service for editing MUGEN screenpacks (menu themes) and motifs.
/// </summary>
public interface IScreenpackEditorService
{
    /// <summary>
    /// Creates a new screenpack from scratch or template.
    /// </summary>
    Task<Result<ScreenpackCreationResult>> CreateScreenpackAsync(
        ScreenpackCreationRequest request, 
        CancellationToken ct = default);

    /// <summary>
    /// Loads an existing screenpack for editing.
    /// </summary>
    Task<Result<ScreenpackData>> LoadScreenpackAsync(
        string screenpackPath, 
        CancellationToken ct = default);

    /// <summary>
    /// Updates screenpack colors and visual theme.
    /// </summary>
    Task<Result> UpdateScreenpackThemeAsync(
        string screenpackPath, 
        ScreenpackTheme theme, 
        CancellationToken ct = default);

    /// <summary>
    /// Updates menu fonts.
    /// </summary>
    Task<Result> UpdateFontsAsync(
        string screenpackPath, 
        FontConfiguration fonts, 
        CancellationToken ct = default);

    /// <summary>
    /// Updates menu layout and positioning.
    /// </summary>
    Task<Result> UpdateMenuLayoutAsync(
        string screenpackPath, 
        MenuLayout layout, 
        CancellationToken ct = default);

    /// <summary>
    /// Updates background animations and effects.
    /// </summary>
    Task<Result> UpdateBackgroundEffectsAsync(
        string screenpackPath, 
        BackgroundEffects effects, 
        CancellationToken ct = default);

    /// <summary>
    /// Exports screenpack to distribution format.
    /// </summary>
    Task<Result<string>> ExportScreenpackAsync(
        string screenpackPath, 
        string outputDirectory, 
        CancellationToken ct = default);

    /// <summary>
    /// Previews screenpack changes in real-time.
    /// </summary>
    Task<Result<ScreenpackPreview>> GeneratePreviewAsync(
        string screenpackPath, 
        CancellationToken ct = default);

    /// <summary>
    /// Gets available screenpack templates.
    /// </summary>
    Task<Result<IReadOnlyList<ScreenpackTemplate>>> GetTemplatesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Validates screenpack configuration.
    /// </summary>
    Task<Result<ValidationResult>> ValidateScreenpackAsync(
        string screenpackPath, 
        CancellationToken ct = default);
}

/// <summary>
/// Request to create a new screenpack.
/// </summary>
public record ScreenpackCreationRequest(
    string Name,
    string Author,
    string Description,
    string BaseTemplate,
    ScreenpackResolution Resolution,
    ScreenpackTheme InitialTheme,
    bool IncludeAnimatedBackground,
    bool IncludeCustomSounds);

/// <summary>
/// Screenpack resolution settings.
/// </summary>
public record ScreenpackResolution(
    int Width,
    int Height,
    bool SupportWidescreen,
    bool Support4K);

/// <summary>
/// Screenpack visual theme.
/// </summary>
public record ScreenpackTheme(
    string Name,
    MugenColor PrimaryColor,
    MugenColor SecondaryColor,
    MugenColor AccentColor,
    MugenColor BackgroundColor,
    MugenColor TextColor,
    MugenColor SelectionColor,
    BackgroundType BackgroundType,
    string? BackgroundImagePath,
    AnimationType MenuAnimation,
    TransitionType TransitionStyle);

/// <summary>
/// Background types for screenpacks.
/// </summary>
public enum BackgroundType
{
    Static,
    Animated,
    Parallax,
    Video,
    Shader
}

/// <summary>
/// Menu animation types.
/// </summary>
public enum AnimationType
{
    None,
    Fade,
    Slide,
    Scale,
    Bounce,
    Rotate
}

/// <summary>
/// Transition styles.
/// </summary>
public enum TransitionType
{
    Instant,
    Fade,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,
    Zoom,
    Wipe
}

/// <summary>
/// Screenpack color definition.
/// </summary>
public record MugenColor(byte R, byte G, byte B);

/// <summary>
/// Font configuration.
/// </summary>
public record FontConfiguration(
    string MenuFontName,
    int MenuFontSize,
    string TitleFontName,
    int TitleFontSize,
    string MessageFontName,
    int MessageFontSize,
    FontStyle DefaultStyle,
    bool AntiAliasing,
    bool Shadow,
    MugenColor ShadowColor,
    int ShadowOffsetX,
    int ShadowOffsetY);

/// <summary>
/// Font styles.
/// </summary>
public enum FontStyle
{
    Regular,
    Bold,
    Italic,
    BoldItalic
}

/// <summary>
/// Menu layout configuration.
/// </summary>
public record MenuLayout(
    int MenuX,
    int MenuY,
    int MenuItemSpacing,
    MenuAlignment Alignment,
    int TitleX,
    int TitleY,
    bool ShowVersion,
    int VersionX,
    int VersionY,
    bool ShowLogo,
    int LogoX,
    int LogoY,
    int LogoScalePercent);

/// <summary>
/// Menu alignment options.
/// </summary>
public enum MenuAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// Background effects configuration.
/// </summary>
public record BackgroundEffects(
    bool EnableParticles,
    ParticleType ParticleType,
    int ParticleCount,
    MugenColor ParticleColor,
    bool EnableMusic,
    string? MusicPath,
    bool EnableSoundEffects,
    float MusicVolume,
    float SFXVolume,
    bool EnableAnimation,
    string? AnimationPath,
    int AnimationSpeed);

/// <summary>
/// Particle types.
/// </summary>
public enum ParticleType
{
    None,
    Snow,
    Rain,
    Stars,
    Fireflies,
    Confetti,
    Custom
}

/// <summary>
/// Result of screenpack creation.
/// </summary>
public record ScreenpackCreationResult(
    string Name,
    string FilePath,
    IReadOnlyList<string> GeneratedFiles,
    bool Success);

/// <summary>
/// Screenpack data for editing.
/// </summary>
public record ScreenpackData(
    string Name,
    string Path,
    string Author,
    string Version,
    ScreenpackResolution Resolution,
    ScreenpackTheme Theme,
    FontConfiguration Fonts,
    MenuLayout Layout,
    BackgroundEffects Effects,
    IReadOnlyList<string> IncludedFiles,
    DateTime LastModified);

/// <summary>
/// Screenpack preview data.
/// </summary>
public record ScreenpackPreview(
    string ScreenpackName,
    byte[] PreviewImage,
    string ThemeName,
    int ResolutionWidth,
    int ResolutionHeight);

/// <summary>
/// Screenpack template information.
/// </summary>
public record ScreenpackTemplate(
    string Id,
    string Name,
    string Description,
    string Author,
    string PreviewImagePath,
    ScreenpackResolution Resolution,
    bool IsAnimated,
    IReadOnlyList<string> Tags);
