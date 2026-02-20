using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.SoundDesign;

/// <summary>
/// Manages sound effect operations for the SoundDesignService.
/// </summary>
public class SoundEffectManager
{
    private readonly ILogger<SoundEffectManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<Guid, SoundEffect> _soundEffects;

    public SoundEffectManager(
        ILogger<SoundEffectManager> logger,
        ITimeProvider timeProvider,
        ConcurrentDictionary<Guid, SoundEffect> soundEffects)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _soundEffects = soundEffects;
    }

    public async Task<Result<IReadOnlyList<SoundEffect>>> LoadSoundEffectsAsync(
        string directoryPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Loading sound effects from: {Directory}", directoryPath);

            if (!Directory.Exists(directoryPath))
            {
                return Result<IReadOnlyList<SoundEffect>>.Failure(
                    $"Directory not found: {directoryPath}", ErrorType.NotFound);
            }

            var sounds = new List<SoundEffect>();
            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => IsAudioFile(f));

            foreach (var file in files)
            {
                var sound = await LoadSingleSoundAsync(file, ct);
                if (sound != null)
                {
                    sounds.Add(sound);
                    _soundEffects[sound.Id] = sound;
                }
            }

            _logger.LogInformation("Loaded {Count} sound effects", sounds.Count);
            return Result<IReadOnlyList<SoundEffect>>.Success(sounds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sound effects");
            return Result<IReadOnlyList<SoundEffect>>.Failure(
                $"Load failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<SoundEffect>> ImportSoundEffectAsync(
        string filePath,
        SoundEffectMetadata metadata,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Importing sound effect: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return Result<SoundEffect>.Failure($"File not found: {filePath}", ErrorType.NotFound);
            }

            var sound = await LoadSingleSoundAsync(filePath, ct, metadata);
            if (sound == null)
            {
                return Result<SoundEffect>.Failure("Failed to load sound", ErrorType.Internal);
            }

            _soundEffects[sound.Id] = sound;
            return Result<SoundEffect>.Success(sound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import sound effect");
            return Result<SoundEffect>.Failure($"Import failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<SoundEffect>> GetSoundEffectAsync(Guid id, CancellationToken ct = default)
    {
        if (_soundEffects.TryGetValue(id, out var sound))
        {
            return Task.FromResult(Result<SoundEffect>.Success(sound));
        }

        return Task.FromResult(Result<SoundEffect>.Failure($"Sound effect {id} not found", ErrorType.NotFound));
    }

    public Task<Result> DeleteSoundEffectAsync(Guid id, CancellationToken ct = default)
    {
        _soundEffects.TryRemove(id, out _);
        return Task.FromResult(Result.Success());
    }

    private static bool IsAudioFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".wav" or ".mp3" or ".ogg" or ".flac" or ".wma";
    }

    private Task<SoundEffect?> LoadSingleSoundAsync(
        string filePath,
        CancellationToken ct,
        SoundEffectMetadata? metadata = null)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var format = GetAudioFormat(fileInfo.Extension);

            return Task.FromResult<SoundEffect?>(new SoundEffect(
                Guid.NewGuid(),
                Path.GetFileNameWithoutExtension(filePath),
                filePath,
                format,
                44100,
                2,
                TimeSpan.FromSeconds(1),
                fileInfo.Length,
                metadata ?? new SoundEffectMetadata(
                    null,
                    null,
                    new List<string>(),
                    SoundUsage.Custom,
                    null,
                    null,
                    1.0),
                _timeProvider.UtcNow,
                _timeProvider.UtcNow));
        }
        catch
        {
            return Task.FromResult<SoundEffect?>(null);
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
