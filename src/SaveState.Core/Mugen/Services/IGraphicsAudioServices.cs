using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Mugen.Services;

public interface IMugenGraphicsEngine
{
    Task<Result> ApplyDynamicLightingAsync(string target, ScreenFilterConfig config, CancellationToken cancellationToken = default);
    Task<Result> ApplyScreenFilterAsync(ScreenFilterType filterType, ScreenFilterConfig config, CancellationToken cancellationToken = default);
}

public interface IMugenSoundDesignStudio
{
    Task<Result<AudioAnalysis>> AnalyzeAudioAsync(string filePath, CancellationToken cancellationToken = default);
    Task<Result> ApplyAudioMixAsync(AudioMixConfig config, CancellationToken cancellationToken = default);
}
