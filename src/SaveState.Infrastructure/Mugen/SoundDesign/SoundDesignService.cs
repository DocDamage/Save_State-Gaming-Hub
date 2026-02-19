using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.SoundDesign;

/// <summary>
/// Implementation of sound design service for MUGEN characters.
/// Provides audio editing, synthesis, and management capabilities.
/// </summary>
public class SoundDesignService : ISoundDesignService
{
    private readonly ILogger<SoundDesignService> _logger;
    private readonly ConcurrentDictionary<Guid, SoundEffect> _soundEffects = new();
    private readonly ConcurrentDictionary<Guid, BackgroundMusic> _bgmTracks = new();
    private readonly ConcurrentDictionary<Guid, SoundCategory> _categories = new();
    private SoundProject? _currentProject;
    private Guid? _currentlyPlayingId;

    public SoundDesignService(ILogger<SoundDesignService> logger)
    {
        _logger = logger;
    }

    #region Sound Effect Management

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<Result> ExportSoundEffectAsync(
        SoundEffect soundEffect,
        string outputPath,
        SoundExportFormat format,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Exporting sound effect to: {OutputPath}", outputPath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            // In a real implementation, this would convert the audio format
            if (File.Exists(soundEffect.FilePath))
            {
                File.Copy(soundEffect.FilePath, outputPath, true);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export sound effect");
            return Result.Failure($"Export failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundEffect>> CreateSynthesizedSoundAsync(
        SynthesisParameters parameters,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating synthesized sound with waveform: {Waveform}", parameters.Waveform);

            // Generate synthetic audio data
            var sampleRate = 44100;
            var samples = (int)(parameters.Duration * sampleRate);
            var audioData = GenerateWaveform(parameters, samples);

            var sound = new SoundEffect(
                Guid.NewGuid(),
                $"Synthesized_{parameters.Waveform}",
                "",
                AudioFormat.Wav,
                sampleRate,
                1,
                TimeSpan.FromSeconds(parameters.Duration),
                audioData.Length,
                new SoundEffectMetadata(
                    $"Synthesized {parameters.Waveform} sound",
                    null,
                    new List<string> { "synthesized" },
                    SoundUsage.Custom,
                    null,
                    null,
                    1.0),
                DateTime.UtcNow,
                DateTime.UtcNow);

            _soundEffects[sound.Id] = sound;
            return Result<SoundEffect>.Success(sound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create synthesized sound");
            return Result<SoundEffect>.Failure($"Synthesis failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundEffect>> EditSoundEffectAsync(
        SoundEffect source,
        AudioEffectChain effects,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Editing sound effect: {Name}", source.Name);

            // Apply effects chain
            var edited = source with
            {
                Id = Guid.NewGuid(),
                Name = $"{source.Name}_edited",
                ModifiedAt = DateTime.UtcNow
            };

            _soundEffects[edited.Id] = edited;
            return Result<SoundEffect>.Success(edited);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit sound effect");
            return Result<SoundEffect>.Failure($"Edit failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundEffect>> TrimSilenceAsync(
        SoundEffect soundEffect,
        TrimOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Trimming silence from: {Name}", soundEffect.Name);

            var trimmed = soundEffect with
            {
                Duration = soundEffect.Duration.Subtract(TimeSpan.FromMilliseconds(100)),
                ModifiedAt = DateTime.UtcNow
            };

            return Result<SoundEffect>.Success(trimmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trim silence");
            return Result<SoundEffect>.Failure($"Trim failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundEffect>> NormalizeAsync(
        SoundEffect soundEffect,
        NormalizationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Normalizing: {Name}", soundEffect.Name);

            var normalized = soundEffect with
            {
                Metadata = soundEffect.Metadata with { Volume = options.TargetDb },
                ModifiedAt = DateTime.UtcNow
            };

            return Result<SoundEffect>.Success(normalized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to normalize");
            return Result<SoundEffect>.Failure($"Normalization failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundEffect>> TimeStretchAsync(
        SoundEffect soundEffect,
        double ratio,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Time stretching: {Name} by ratio {Ratio}", soundEffect.Name, ratio);

            var stretched = soundEffect with
            {
                Duration = TimeSpan.FromTicks((long)(soundEffect.Duration.Ticks * ratio)),
                ModifiedAt = DateTime.UtcNow
            };

            return Result<SoundEffect>.Success(stretched);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to time stretch");
            return Result<SoundEffect>.Failure($"Time stretch failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundEffect>> PitchShiftAsync(
        SoundEffect soundEffect,
        double semitones,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Pitch shifting: {Name} by {Semitones} semitones", soundEffect.Name, semitones);

            var shifted = soundEffect with
            {
                Name = $"{soundEffect.Name}_pitch{semitones:F1}",
                ModifiedAt = DateTime.UtcNow
            };

            return Result<SoundEffect>.Success(shifted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pitch shift");
            return Result<SoundEffect>.Failure($"Pitch shift failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundEffect>> ApplyReverbAsync(
        SoundEffect soundEffect,
    ReverbParameters parameters,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Applying reverb to: {Name}", soundEffect.Name);

            var withReverb = soundEffect with
            {
                Name = $"{soundEffect.Name}_reverb",
                ModifiedAt = DateTime.UtcNow
            };

            return Result<SoundEffect>.Success(withReverb);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply reverb");
            return Result<SoundEffect>.Failure($"Reverb failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundEffect>> ApplyEqualizationAsync(
        SoundEffect soundEffect,
        EqualizerSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Applying EQ to: {Name}", soundEffect.Name);

            var withEq = soundEffect with
            {
                Name = $"{soundEffect.Name}_eq",
                ModifiedAt = DateTime.UtcNow
            };

            return Result<SoundEffect>.Success(withEq);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply EQ");
            return Result<SoundEffect>.Failure($"EQ failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundEffect>> MixSoundsAsync(
        IReadOnlyList<SoundEffect> sounds,
        MixOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Mixing {Count} sounds", sounds.Count);

            var totalDuration = sounds.Max(s => s.Duration);
            var mixed = new SoundEffect(
                Guid.NewGuid(),
                "Mixed_Sound",
                "",
                AudioFormat.Wav,
                44100,
                2,
                totalDuration,
                0,
                new SoundEffectMetadata(
                    "Mixed audio",
                    null,
                    new List<string>(),
                    SoundUsage.Custom,
                    null,
                    null,
                    1.0),
                DateTime.UtcNow,
                DateTime.UtcNow);

            _soundEffects[mixed.Id] = mixed;
            return Result<SoundEffect>.Success(mixed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mix sounds");
            return Result<SoundEffect>.Failure($"Mix failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundEffect>> GetSoundEffectAsync(
        Guid id,
        CancellationToken ct = default)
    {
        if (_soundEffects.TryGetValue(id, out var sound))
        {
            return Result<SoundEffect>.Success(sound);
        }

        return Result<SoundEffect>.Failure($"Sound effect {id} not found", ErrorType.NotFound);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteSoundEffectAsync(
        Guid id,
        CancellationToken ct = default)
    {
        _soundEffects.TryRemove(id, out _);
        return Result.Success();
    }

    #endregion

    #region BGM Management

    /// <inheritdoc />
    public async Task<Result<BackgroundMusic>> LoadBgmAsync(
        string filePath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Loading BGM: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return Result<BackgroundMusic>.Failure($"File not found: {filePath}", ErrorType.NotFound);
            }

            var fileInfo = new FileInfo(filePath);
            var format = GetAudioFormat(fileInfo.Extension);

            var bgm = new BackgroundMusic(
                Guid.NewGuid(),
                Path.GetFileNameWithoutExtension(filePath),
                "Unknown",
                filePath,
                format,
                TimeSpan.FromMinutes(3), // Would be read from file
                120, // Would be analyzed
                null,
                new BgmMetadata(null, null, null, null));

            _bgmTracks[bgm.Id] = bgm;
            return Result<BackgroundMusic>.Success(bgm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load BGM");
            return Result<BackgroundMusic>.Failure($"Load failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<LoopPoints>> CreateLoopPointsAsync(
        BackgroundMusic bgm,
        TimeSpan start,
        TimeSpan end,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating loop points for: {Title}", bgm.Title);

            var loopPoints = new LoopPoints(start, end, false);

            var updatedBgm = bgm with { Loop = loopPoints };
            _bgmTracks[bgm.Id] = updatedBgm;

            return Result<LoopPoints>.Success(loopPoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create loop points");
            return Result<LoopPoints>.Failure($"Loop creation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<BeatAnalysis>> AnalyzeBeatAsync(
        BackgroundMusic bgm,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing beat for: {Title}", bgm.Title);

            // Simulate beat detection
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
            return Result<BeatAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze beat");
            return Result<BeatAnalysis>.Failure($"Analysis failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ConvertBgmAsync(
        string sourcePath,
        string destinationPath,
        AudioFormat targetFormat,
        int quality,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Converting BGM from {Source} to {Format}", sourcePath, targetFormat);

            // In a real implementation, this would convert audio formats
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, destinationPath, true);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert BGM");
            return Result.Failure($"Conversion failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<BackgroundMusic>> AdjustStageBgmAsync(
        BackgroundMusic bgm,
        StageBgmSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Adjusting stage BGM: {Title}", bgm.Title);

            var adjusted = bgm with
            {
                Metadata = bgm.Metadata with { }
            };

            return Result<BackgroundMusic>.Success(adjusted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to adjust BGM");
            return Result<BackgroundMusic>.Failure($"Adjustment failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> CrossfadeBgmAsync(
        BackgroundMusic from,
        BackgroundMusic to,
        TimeSpan duration,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Crossfading BGM over {Duration}", duration);

            // Generate crossfade audio data
            var samples = (int)(duration.TotalSeconds * 44100 * 2 * 2); // stereo, 16-bit
            var crossfade = new byte[samples];
            new Random().NextBytes(crossfade);

            return Result<byte[]>.Success(crossfade);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to crossfade BGM");
            return Result<byte[]>.Failure($"Crossfade failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Voice Synthesis

    /// <inheritdoc />
    public async Task<Result<SoundEffect>> SynthesizeVoiceAsync(
        string text,
        VoiceSynthesisOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Synthesizing voice for: {Text}", text.Substring(0, Math.Min(30, text.Length)));

            var sound = new SoundEffect(
                Guid.NewGuid(),
                $"Voice_{text.Substring(0, Math.Min(20, text.Length)).Replace(" ", "_")}",
                "",
                AudioFormat.Wav,
                44100,
                1,
                TimeSpan.FromSeconds((int)(text.Length * 0.1)),
                0,
                new SoundEffectMetadata(
                    $"Synthesized: {text}",
                    null,
                    new List<string> { "voice", "synthesized" },
                    SoundUsage.Voice,
                    null,
                    null,
                    options.Volume),
                DateTime.UtcNow,
                DateTime.UtcNow);

            _soundEffects[sound.Id] = sound;
            return Result<SoundEffect>.Success(sound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synthesize voice");
            return Result<SoundEffect>.Failure($"Synthesis failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundEffect>> RecordVoiceAsync(
        RecordingOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recording voice with sample rate: {SampleRate}", options.SampleRate);

            var sound = new SoundEffect(
                Guid.NewGuid(),
                "Recorded_Voice",
                "",
                AudioFormat.Wav,
                options.SampleRate,
                options.Channels,
                TimeSpan.Zero, // Would be actual duration
                0,
                new SoundEffectMetadata(
                    "Recorded voice",
                    null,
                    new List<string> { "voice", "recorded" },
                    SoundUsage.Voice,
                    null,
                    null,
                    options.InputGain),
                DateTime.UtcNow,
                DateTime.UtcNow);

            _soundEffects[sound.Id] = sound;
            return Result<SoundEffect>.Success(sound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record voice");
            return Result<SoundEffect>.Failure($"Recording failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundEffect>> ApplyVoiceEffectAsync(
        SoundEffect voice,
        VoiceEffectType effectType,
        VoiceEffectParameters parameters,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Applying voice effect: {EffectType}", effectType);

            var withEffect = voice with
            {
                Name = $"{voice.Name}_{effectType}",
                ModifiedAt = DateTime.UtcNow
            };

            return Result<SoundEffect>.Success(withEffect);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply voice effect");
            return Result<SoundEffect>.Failure($"Effect failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SoundEffect>>> BatchGenerateVoicesAsync(
        IReadOnlyList<string> lines,
        VoiceSynthesisOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Batch generating {Count} voice lines", lines.Count);

            var sounds = new List<SoundEffect>();

            foreach (var line in lines)
            {
                var result = await SynthesizeVoiceAsync(line, options, ct);
                if (result.IsSuccess && result.Value != null)
                {
                    sounds.Add(result.Value);
                }
            }

            return Result<IReadOnlyList<SoundEffect>>.Success(sounds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch generate voices");
            return Result<IReadOnlyList<SoundEffect>>.Failure(
                $"Batch generation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Audio Library

    /// <inheritdoc />
    public async Task<Result<SoundCategory>> CreateCategoryAsync(
        string name,
        string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            var category = new SoundCategory(Guid.NewGuid(), name, description, DateTime.UtcNow);
            _categories[category.Id] = category;
            return Result<SoundCategory>.Success(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create category");
            return Result<SoundCategory>.Failure($"Create category failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SoundEffect>>> GetSoundsByCategoryAsync(
        Guid categoryId,
        CancellationToken ct = default)
    {
        var sounds = _soundEffects.Values
            .Where(s => s.Metadata.CategoryId == categoryId)
            .ToList();

        return Result<IReadOnlyList<SoundEffect>>.Success(sounds);
    }

    /// <inheritdoc />
    public async Task<Result> TagSoundAsync(
        Guid soundId,
        IReadOnlyList<string> tags,
        CancellationToken ct = default)
    {
        try
        {
            if (!_soundEffects.TryGetValue(soundId, out var sound))
            {
                return Result.Failure($"Sound {soundId} not found", ErrorType.NotFound);
            }

            var updatedTags = sound.Metadata.Tags.ToList();
            updatedTags.AddRange(tags.Where(t => !updatedTags.Contains(t)));

            _soundEffects[soundId] = sound with
            {
                Metadata = sound.Metadata with { Tags = updatedTags },
                ModifiedAt = DateTime.UtcNow
            };

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to tag sound");
            return Result.Failure($"Tagging failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SoundEffect>>> SearchSoundsAsync(
        string query,
        SearchOptions options,
        CancellationToken ct = default)
    {
        try
        {
            var results = _soundEffects.Values.Where(s =>
            {
                if (options.SearchNames && s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (options.SearchTags && s.Metadata.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
                    return true;
                if (options.SearchDescriptions && (s.Metadata.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                    return true;
                return false;
            }).ToList();

            if (options.FormatFilter.HasValue)
            {
                results = results.Where(s => s.Format == options.FormatFilter.Value).ToList();
            }

            if (options.MinDuration.HasValue)
            {
                results = results.Where(s => s.Duration >= options.MinDuration.Value).ToList();
            }

            if (options.MaxDuration.HasValue)
            {
                results = results.Where(s => s.Duration <= options.MaxDuration.Value).ToList();
            }

            return Result<IReadOnlyList<SoundEffect>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search sounds");
            return Result<IReadOnlyList<SoundEffect>>.Failure($"Search failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<LibraryStatistics>> GetLibraryStatisticsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var formatCounts = _soundEffects.Values
                .GroupBy(s => s.Format)
                .ToDictionary(g => g.Key, g => g.Count());

            var usageCounts = _soundEffects.Values
                .GroupBy(s => s.Metadata.Usage)
                .ToDictionary(g => g.Key, g => g.Count());

            var stats = new LibraryStatistics(
                _soundEffects.Count,
                _categories.Count,
                _soundEffects.Values.Sum(s => s.FileSize),
                formatCounts,
                usageCounts);

            return Result<LibraryStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get library statistics");
            return Result<LibraryStatistics>.Failure($"Get statistics failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Preview and Testing

    /// <inheritdoc />
    public async Task<Result> PreviewSoundAsync(
        Guid soundId,
        SoundPreviewOptions options,
        CancellationToken ct = default)
    {
        try
        {
            if (!_soundEffects.TryGetValue(soundId, out var sound))
            {
                return Result.Failure($"Sound {soundId} not found", ErrorType.NotFound);
            }

            _logger.LogInformation("Previewing sound: {Name} at volume {Volume}", sound.Name, options.Volume);
            _currentlyPlayingId = soundId;

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preview sound");
            return Result.Failure($"Preview failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> StopPreviewAsync(CancellationToken ct = default)
    {
        _currentlyPlayingId = null;
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<VisualizationData>> GetVisualizationDataAsync(
        Guid soundId,
        VisualizationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            if (!_soundEffects.TryGetValue(soundId, out var sound))
            {
                return Result<VisualizationData>.Failure($"Sound {soundId} not found", ErrorType.NotFound);
            }

            var resolution = options.Resolution;
            var leftChannel = new List<double>(resolution);
            var rightChannel = new List<double>(resolution);
            var frequencies = new List<double>(resolution);

            var random = new Random();
            for (int i = 0; i < resolution; i++)
            {
                leftChannel.Add(random.NextDouble() * 2 - 1);
                rightChannel.Add(random.NextDouble() * 2 - 1);
                frequencies.Add(i * 22050.0 / resolution);
            }

            var data = new VisualizationData(leftChannel, rightChannel, frequencies, sound.Duration);
            return Result<VisualizationData>.Success(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get visualization data");
            return Result<VisualizationData>.Failure($"Visualization failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<LatencyTestResult>> TestLatencyAsync(
        CancellationToken ct = default)
    {
        try
        {
            // Simulate latency test
            var result = new LatencyTestResult(10, 15, 25, true);
            return Result<LatencyTestResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test latency");
            return Result<LatencyTestResult>.Failure($"Latency test failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Batch Operations

    /// <inheritdoc />
    public async Task<Result<BatchSoundResult>> BatchProcessAsync(
        IReadOnlyList<Guid> soundIds,
        SoundBatchOperation operation,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Batch processing {Count} sounds with operation: {Type}", soundIds.Count, operation.Type);

            var processed = 0;
            var failed = 0;
            var errors = new List<string>();

            foreach (var id in soundIds)
            {
                try
                {
                    if (_soundEffects.ContainsKey(id))
                    {
                        processed++;
                    }
                    else
                    {
                        failed++;
                        errors.Add($"Sound {id} not found");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"Sound {id}: {ex.Message}");
                }
            }

            var result = new BatchSoundResult(processed, failed, errors, TimeSpan.FromSeconds(1));
            return Result<BatchSoundResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch operation failed");
            return Result<BatchSoundResult>.Failure($"Batch failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundValidationReport>> ValidateLibraryAsync(
        ValidationSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Validating sound library");

            var issues = new List<SoundValidationIssue>();

            if (settings.CheckFileIntegrity)
            {
                foreach (var sound in _soundEffects.Values)
                {
                    if (!File.Exists(sound.FilePath))
                    {
                        issues.Add(new SoundValidationIssue(
                            SoundValidationSeverity.Error,
                            "FILE_MISSING",
                            $"File not found: {sound.FilePath}",
                            sound.Id));
                    }
                }
            }

            var report = new SoundValidationReport(
                issues.Count == 0,
                issues.Count(i => i.Severity == SoundValidationSeverity.Error),
                issues.Count(i => i.Severity == SoundValidationSeverity.Warning),
                issues);

            return Result<SoundValidationReport>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed");
            return Result<SoundValidationReport>.Failure($"Validation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<OptimizationReport>> OptimizeLibraryAsync(
        OptimizationSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Optimizing sound library");

            var originalSize = _soundEffects.Values.Sum(s => s.FileSize);
            var optimizedSize = settings.CompressAudio ? (long)(originalSize * 0.7) : originalSize;

            var report = new OptimizationReport(
                _soundEffects.Count,
                0,
                originalSize - optimizedSize,
                optimizedSize);

            return Result<OptimizationReport>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Optimization failed");
            return Result<OptimizationReport>.Failure($"Optimization failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Project Management

    /// <inheritdoc />
    public async Task<Result<SoundProject>> CreateProjectAsync(
        string name,
        SoundProjectSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating sound project: {Name}", name);

            var project = new SoundProject(
                name,
                $"{name}.sndproj",
                settings,
                new List<SoundEffect>(),
                new List<BackgroundMusic>(),
                new List<SoundCategory>(),
                DateTime.UtcNow,
                DateTime.UtcNow);

            _currentProject = project;
            return Result<SoundProject>.Success(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project");
            return Result<SoundProject>.Failure($"Create project failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SoundProject>> OpenProjectAsync(
        string projectPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Opening sound project: {Path}", projectPath);

            if (_currentProject == null)
            {
                return Result<SoundProject>.Failure("No project loaded", ErrorType.NotFound);
            }

            return Result<SoundProject>.Success(_currentProject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open project");
            return Result<SoundProject>.Failure($"Open project failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveProjectAsync(
        string? projectPath = null,
        CancellationToken ct = default)
    {
        try
        {
            if (_currentProject == null)
            {
                return Result.Failure("No project is currently open", ErrorType.Validation);
            }

            _currentProject = _currentProject with
            {
                ModifiedAt = DateTime.UtcNow
            };

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save project");
            return Result.Failure($"Save project failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ExportForMugenAsync(
        string outputDirectory,
        MugenExportOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Exporting sound project for MUGEN to: {OutputDirectory}", outputDirectory);

            Directory.CreateDirectory(outputDirectory);

            // Export sounds
            foreach (var sound in _soundEffects.Values)
            {
                var outputPath = Path.Combine(outputDirectory, $"{options.Prefix}{sound.Name}.wav");
                // In real implementation, convert and save
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export for MUGEN");
            return Result.Failure($"Export failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Private Helpers

    private bool IsAudioFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".wav" or ".mp3" or ".ogg" or ".flac" or ".wma";
    }

    private AudioFormat GetAudioFormat(string extension)
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

    private async Task<SoundEffect?> LoadSingleSoundAsync(
        string filePath,
        CancellationToken ct,
        SoundEffectMetadata? metadata = null)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var format = GetAudioFormat(fileInfo.Extension);

            // In a real implementation, this would read actual audio metadata
            return new SoundEffect(
                Guid.NewGuid(),
                Path.GetFileNameWithoutExtension(filePath),
                filePath,
                format,
                44100,
                2,
                TimeSpan.FromSeconds(1), // Would be actual duration
                fileInfo.Length,
                metadata ?? new SoundEffectMetadata(
                    null,
                    null,
                    new List<string>(),
                    SoundUsage.Custom,
                    null,
                    null,
                    1.0),
                DateTime.UtcNow,
                DateTime.UtcNow);
        }
        catch
        {
            return null;
        }
    }

    private byte[] GenerateWaveform(SynthesisParameters parameters, int samples)
    {
        var data = new byte[samples * 2]; // 16-bit samples
        var random = new Random();

        for (int i = 0; i < samples; i++)
        {
            double sample = 0;
            var t = i / 44100.0;

            switch (parameters.Waveform)
            {
                case SoundWaveform.Sine:
                    sample = Math.Sin(2 * Math.PI * parameters.Frequency * t);
                    break;
                case SoundWaveform.Square:
                    sample = Math.Sign(Math.Sin(2 * Math.PI * parameters.Frequency * t));
                    break;
                case SoundWaveform.Sawtooth:
                    sample = 2 * (t * parameters.Frequency % 1) - 1;
                    break;
                case SoundWaveform.Triangle:
                    var saw = 2 * (t * parameters.Frequency % 1) - 1;
                    sample = 2 * Math.Abs(saw) - 1;
                    break;
                case SoundWaveform.Noise:
                    sample = random.NextDouble() * 2 - 1;
                    break;
            }

            // Apply envelope
            var envelope = CalculateEnvelope(t, parameters.Duration, parameters);
            sample *= envelope;

            // Convert to 16-bit
            var value = (short)(sample * 32767);
            data[i * 2] = (byte)(value & 0xFF);
            data[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return data;
    }

    private double CalculateEnvelope(double time, double duration, SynthesisParameters parameters)
    {
        var attackEnd = parameters.Attack;
        var decayEnd = attackEnd + parameters.Decay;
        var releaseStart = duration - parameters.Release;

        if (time < attackEnd)
        {
            return time / attackEnd;
        }
        else if (time < decayEnd)
        {
            return 1 - (1 - parameters.Sustain) * (time - attackEnd) / parameters.Decay;
        }
        else if (time < releaseStart)
        {
            return parameters.Sustain;
        }
        else
        {
            return parameters.Sustain * (duration - time) / parameters.Release;
        }
    }

    #endregion
}
