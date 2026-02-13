using SaveState.Core.Common;
using SaveState.Core.Translation.Models;

namespace SaveState.Core.Translation.Services;

/// <summary>
/// Service that provides real-time translation of in-game text using OCR and voice dubbing.
/// </summary>
public interface IRealTimeTranslationService
{
    /// <summary>
    /// Initializes the translation service.
    /// </summary>
    /// <param name="configuration">Translation configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> InitializeAsync(TranslationConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Performs OCR on a screen image and extracts text.
    /// </summary>
    /// <param name="imageData">Screen capture image data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing OCR results.</returns>
    Task<Result<OcrResult>> PerformOcrAsync(byte[] imageData, CancellationToken ct = default);

    /// <summary>
    /// Translates text from source language to target language.
    /// </summary>
    /// <param name="text">Text to translate.</param>
    /// <param name="sourceLanguage">Source language code (or "auto" for detection).</param>
    /// <param name="targetLanguage">Target language code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the translation.</returns>
    Task<Result<TranslationResult>> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken ct = default);

    /// <summary>
    /// Translates text using game context for better accuracy.
    /// </summary>
    /// <param name="text">Text to translate.</param>
    /// <param name="sourceLanguage">Source language code.</param>
    /// <param name="targetLanguage">Target language code.</param>
    /// <param name="gameContext">Game context for specialized translation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the translation.</returns>
    Task<Result<TranslationResult>> TranslateWithContextAsync(string text, string sourceLanguage, string targetLanguage, string gameContext, CancellationToken ct = default);

    /// <summary>
    /// Captures screen, performs OCR, and translates detected text.
    /// </summary>
    /// <param name="screenImage">Screen capture image.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing captured and translated text.</returns>
    Task<Result<ScreenTextCapture>> CaptureAndTranslateAsync(byte[] screenImage, CancellationToken ct = default);

    /// <summary>
    /// Generates voice dubbing for translated text.
    /// </summary>
    /// <param name="text">Text to dub.</param>
    /// <param name="targetLanguage">Target language code.</param>
    /// <param name="voiceProfile">Voice profile to use.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the voice dubbing data.</returns>
    Task<Result<VoiceDubbingData>> GenerateVoiceDubbingAsync(string text, string targetLanguage, string? voiceProfile = null, CancellationToken ct = default);

    /// <summary>
    /// Adds a translation to the memory.
    /// </summary>
    /// <param name="entry">Translation memory entry.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> AddToTranslationMemoryAsync(TranslationMemoryEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Searches translation memory for similar translations.
    /// </summary>
    /// <param name="text">Text to search for.</param>
    /// <param name="sourceLanguage">Source language code.</param>
    /// <param name="targetLanguage">Target language code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing matching translation entries.</returns>
    Task<Result<IReadOnlyList<TranslationMemoryEntry>>> SearchTranslationMemoryAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken ct = default);

    /// <summary>
    /// Gets all supported languages.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing supported languages.</returns>
    Task<Result<IReadOnlyList<SupportedLanguage>>> GetSupportedLanguagesAsync(CancellationToken ct = default);

    /// <summary>
    /// Detects the language of the given text.
    /// </summary>
    /// <param name="text">Text to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing detected language code.</returns>
    Task<Result<string>> DetectLanguageAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Updates the translation configuration.
    /// </summary>
    /// <param name="configuration">New configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UpdateConfigurationAsync(TranslationConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Starts continuous screen monitoring for translation.
    /// </summary>
    /// <param name="callback">Callback for detected text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> StartScreenMonitoringAsync(Action<ScreenTextCapture> callback, CancellationToken ct = default);

    /// <summary>
    /// Stops continuous screen monitoring.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> StopScreenMonitoringAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets translation statistics for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="periodStart">Start of the period.</param>
    /// <param name="periodEnd">End of the period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing translation statistics.</returns>
    Task<Result<TranslationStatistics>> GetStatisticsAsync(string userId, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);

    /// <summary>
    /// Shuts down the translation service.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ShutdownAsync(CancellationToken ct = default);
}
