using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.SoundDesign;

/// <summary>
/// Manages background music operations for the SoundDesignService.
/// </summary>
public class BgmManager
{
    private readonly ILogger<BgmManager> _logger;
    private readonly ConcurrentDictionary<Guid, BackgroundMusic> _bgmTracks;

    public BgmManager(
        ILogger<BgmManager> logger,
        ConcurrentDictionary<Guid, BackgroundMusic> bgmTracks)
    {
        _logger = logger;
        _bgmTracks = bgmTracks;
    }

    public Task<Result<BackgroundMusic>> LoadBgmAsync(
        string filePath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Loading BGM: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return Task.FromResult(Result<BackgroundMusic>.Failure($"File not found: {filePath}", ErrorType.NotFound));
            }

            var fileInfo = new FileInfo(filePath);
            var format = GetAudioFormat(fileInfo.Extension);

            var bgm = new BackgroundMusic(
                Guid.NewGuid(),
                Path.GetFileNameWithoutExtension(filePath),
                "Unknown",
                filePath,
                format,
                TimeSpan.FromMinutes(3),
                120,
                null,
                new BgmMetadata(null, null, null, null));

            _bgmTracks[bgm.Id] = bgm;
            return Task.FromResult(Result<BackgroundMusic>.Success(bgm));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load BGM");
            return Task.FromResult(Result<BackgroundMusic>.Failure($"Load failed: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<BeatAnalysis>> AnalyzeBeatAsync(
        BackgroundMusic bgm,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing beat for: {Title}", bgm.Title);

            var beats = new List<TimeSpan>();
            var energy = new List<double>();
            var current = TimeSpan.Zero;
            var beatInterval = TimeSpan.FromMilliseconds(500);

            while (current < bgm.Duration)
            {
                beats.Add(current);
                energy.Add(0.5 + 0.5 * Math.Sin(current.TotalSeconds));
                current += beatInterval;
            }

            var analysis = new BeatAnalysis(120, beats, energy);
            return Task.FromResult(Result<BeatAnalysis>.Success(analysis));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze beat");
            return Task.FromResult(Result<BeatAnalysis>.Failure($"Analysis failed: {ex.Message}", ErrorType.Internal));
        }
    }

    private static AudioFormat GetAudioFormat(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".wav" => AudioFormat.Wav,
            ".mp3" => AudioFormat.Mp3,
            ".ogg" => AudioFormat.Ogg,
            ".flac" => AudioFormat.Flac,
            ".wma" => AudioFormat.Wma,
            _ => AudioFormat.Wav
        };
    }
}
