using SaveState.Core.Common;

namespace SaveState.Core.Ai.Voice;

/// <summary>
/// Processes voice audio into text transcriptions.
/// </summary>
public interface IVoiceProcessor
{
    /// <summary>
    /// Transcribes audio data to text.
    /// </summary>
    Task<Result<VoiceTranscription>> TranscribeAsync(
        byte[] audioData,
        string? filename = null,
        VoiceProcessingOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Transcribes audio from a stream.
    /// </summary>
    Task<Result<VoiceTranscription>> TranscribeAsync(
        Stream audioStream,
        string filename,
        VoiceProcessingOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets supported audio formats.
    /// </summary>
    IReadOnlyList<string> SupportedFormats { get; }
}
