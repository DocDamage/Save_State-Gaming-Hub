using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Infrastructure.Mugen;

public class MugenGraphicsEngine : IMugenGraphicsEngine
{
    public Task<Result> ApplyDynamicLightingAsync(string target, ScreenFilterConfig config, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ApplyScreenFilterAsync(ScreenFilterType filterType, ScreenFilterConfig config, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }
}

public class MugenSoundDesignStudio : IMugenSoundDesignStudio
{
    public Task<Result<AudioAnalysis>> AnalyzeAudioAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var analysis = new AudioAnalysis
        {
            Duration = 10,
            PeakLevelDb = -3,
            RmsLevelDb = -12,
            Loudness = new LoudnessInfo { Integrated = -16 }
        };
        return Task.FromResult(Result.Success(analysis));
    }

    public Task<Result> ApplyAudioMixAsync(AudioMixConfig config, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }
}
