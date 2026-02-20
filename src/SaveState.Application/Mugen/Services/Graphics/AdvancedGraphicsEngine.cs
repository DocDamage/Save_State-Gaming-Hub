using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Services.Graphics.Managers;

namespace SaveState.Application.Mugen.Services.Graphics;

/// <summary>
/// Advanced graphics engine providing dynamic lighting, particle effects,
/// and shader-based rendering for cinematic MUGEN experiences.
/// Acts as a coordinator delegating to specialized managers.
/// </summary>
public class AdvancedGraphicsEngine : IAdvancedGraphicsEngine
{
    private readonly ILogger<AdvancedGraphicsEngine> _logger;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;

    // Managers
    private readonly ShaderManager _shaderManager;
    private readonly LightingManager _lightingManager;
    private readonly PostProcessingManager _postProcessingManager;
    private readonly ParticleManager _particleManager;
    private readonly SceneManager _sceneManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedGraphicsEngine"/> class.
    /// </summary>
    public AdvancedGraphicsEngine(
        ILogger<AdvancedGraphicsEngine> logger,
        ICacheService cache,
        ITimeProvider timeProvider,
        ShaderManager shaderManager,
        LightingManager lightingManager,
        PostProcessingManager postProcessingManager,
        ParticleManager particleManager,
        SceneManager sceneManager)
    {
        _logger = logger;
        _cache = cache;
        _timeProvider = timeProvider;
        _shaderManager = shaderManager;
        _lightingManager = lightingManager;
        _postProcessingManager = postProcessingManager;
        _particleManager = particleManager;
        _sceneManager = sceneManager;
    }

    /// <inheritdoc />
    public async Task<Result<GraphicsScene>> CreateSceneAsync(SceneCreationRequest request, CancellationToken ct = default)
    {
        var lightingSetup = await _lightingManager.CreateDefaultLightingAsync(request.LightingPreset, ct);
        return await _sceneManager.CreateSceneAsync(request, lightingSetup, ct);
    }

    /// <inheritdoc />
    public Task<Result<ParticleSystem>> CreateParticleSystemAsync(ParticleSystemRequest request, CancellationToken ct = default)
        => _particleManager.CreateParticleSystemAsync(request, ct);

    /// <inheritdoc />
    public Task<Result<ShaderProgram>> CompileShaderAsync(ShaderCompilationRequest request, CancellationToken ct = default)
        => _shaderManager.CompileShaderAsync(request, ct);

    /// <inheritdoc />
    public Task<Result<LightingSetup>> CreateLightingSetupAsync(LightingSetupRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(Result<LightingSetup>.Success(
            _lightingManager.CreateLightingSetupAsync(request, ct).Result));
    }

    /// <inheritdoc />
    public Task<Result<PostProcessingEffect>> CreatePostProcessingEffectAsync(PostProcessingRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(Result<PostProcessingEffect>.Success(
            _postProcessingManager.CreateEffectAsync(request, ct).Result));
    }

    /// <inheritdoc />
    public async Task<Result> RenderSceneAsync(string sceneId, RenderContext context, CancellationToken ct = default)
    {
        try
        {
            var sceneResult = await _sceneManager.GetSceneAsync(sceneId, ct);
            if (sceneResult.IsFailure)
            {
                return Result.Failure(sceneResult.Error!);
            }

            var scene = sceneResult.Value;
            _logger.LogInformation("Rendering scene {SceneId} with context {Context}", sceneId, context.ContextId);

            // Background rendering
            foreach (var layer in scene.BackgroundLayers)
            {
                await Task.Delay(5, ct);
            }

            // Lighting
            await _lightingManager.ApplyLightingAsync(scene.LightingSetup, context, ct);

            // Particle systems
            foreach (var particleSystemId in scene.ParticleSystems)
            {
                var psResult = await _particleManager.GetParticleSystemAsync(particleSystemId, ct);
                if (psResult.IsSuccess)
                {
                    await _particleManager.RenderAsync(psResult.Value, context, ct);
                }
            }

            // Post-processing
            await _postProcessingManager.ApplyEffectsAsync(scene.PostProcessingEffects, context, ct);

            _logger.LogInformation("Scene rendering completed: {SceneId}", sceneId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering scene {SceneId}", sceneId);
            return Result.Failure($"Failed to render scene: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<RenderStatistics>> GetRenderStatisticsAsync(string sceneId, CancellationToken ct = default)
    {
        var sceneResult = await _sceneManager.GetSceneAsync(sceneId, ct);
        if (sceneResult.IsFailure)
        {
            return Result<RenderStatistics>.Failure(sceneResult.Error!);
        }

        var particleCount = 0;
        foreach (var psId in sceneResult.Value.ParticleSystems)
        {
            var psResult = await _particleManager.GetParticleSystemAsync(psId, ct);
            if (psResult.IsSuccess)
            {
                particleCount += psResult.Value.ParticleCount;
            }
        }

        return await _sceneManager.GetRenderStatisticsAsync(sceneId, particleCount, ct);
    }
}

/// <summary>
/// Advanced Graphics Engine interface.
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
