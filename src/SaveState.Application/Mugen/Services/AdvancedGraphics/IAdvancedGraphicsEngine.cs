using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;

namespace SaveState.Application.Mugen.Services.AdvancedGraphics;

/// <summary>
/// Advanced Graphics Engine interface for cinematic MUGEN experiences.
/// </summary>
public interface IAdvancedGraphicsEngine
{
    Task<Result<GraphicsScene>> CreateSceneAsync(SceneCreationRequest request, CancellationToken ct = default);
    Task<Result<ParticleSystem>> CreateParticleSystemAsync(ParticleSystemRequest request, CancellationToken ct = default);
    Task<Result<ShaderProgram>> CompileShaderAsync(ShaderCompilationRequest request, CancellationToken ct = default);
    Task<Result<LightingSetup>> CreateLightingSetupAsync(LightingSetupRequest request, CancellationToken ct = default);
    Task<Result<PostProcessingEffect>> CreatePostProcessingEffectAsync(PostProcessingRequest request, CancellationToken ct = default);
    Task<Result> RenderSceneAsync(string sceneId, RenderContext context, CancellationToken ct = default);
    Task<Result<RenderStatistics>> GetRenderStatisticsAsync(string sceneId, CancellationToken ct = default);
}