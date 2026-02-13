using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Advanced graphics engine providing dynamic lighting, particle effects,
/// and shader-based rendering for cinematic MUGEN experiences.
/// </summary>
public class AdvancedGraphicsEngine : AdvancedGraphicsEngineIAdvancedGraphicsEngine
{
    private readonly ILogger<AdvancedGraphicsEngine> _logger;
    private readonly ICacheService _cache;
    private readonly Dictionary<string, AdvancedGraphicsEngineGraphicsScene> _activeScenes = new();
    private readonly Dictionary<string, AdvancedGraphicsEngineParticleSystem> _particleSystems = new();
    private readonly Dictionary<string, AdvancedGraphicsEngineShaderProgram> _shaderPrograms = new();
    private readonly AdvancedGraphicsEngineLightingEngine _lightingEngine;
    private readonly AdvancedGraphicsEnginePostProcessingEngine _postProcessingEngine;

    public AdvancedGraphicsEngine(
        ILogger<AdvancedGraphicsEngine> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache)
    {
        _logger = logger;
        _cache = cache;
        _lightingEngine = new AdvancedGraphicsEngineLightingEngine(loggerFactory.CreateLogger<AdvancedGraphicsEngineLightingEngine>());
        _postProcessingEngine = new AdvancedGraphicsEnginePostProcessingEngine(loggerFactory.CreateLogger<AdvancedGraphicsEnginePostProcessingEngine>());

        InitializeDefaultShaders();
        InitializeDefaultParticleSystems();
    }

    public async Task<Result<AdvancedGraphicsEngineGraphicsScene>> CreateSceneAsync(AdvancedGraphicsEngineSceneCreationRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating advanced graphics scene: {Name}", request.Name);

            var scene = new AdvancedGraphicsEngineGraphicsScene
            {
                SceneId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Dimensions = request.Dimensions,
                BackgroundLayers = new List<AdvancedGraphicsEngineBackgroundLayer>(),
                AdvancedGraphicsEngineLightingSetup = await _lightingEngine.CreateDefaultLightingAsync(request.AdvancedGraphicsEngineLightingPreset, ct),
                ParticleSystems = new List<string>(),
                PostProcessingEffects = new List<AdvancedGraphicsEnginePostProcessingEffect>(),
                AdvancedGraphicsEngineCameraSettings = CreateDefaultCameraSettings(),
                AdvancedGraphicsEngineRenderSettings = CreateDefaultRenderSettings(),
                CreatedAt = DateTime.UtcNow
            };

            var backgroundLayers = new List<AdvancedGraphicsEngineBackgroundLayer>();
            foreach (var layerRequest in request.BackgroundLayers)
            {
                var layer = await CreateBackgroundLayerAsync(layerRequest, ct);
                backgroundLayers.Add(layer);
            }
            scene.BackgroundLayers = backgroundLayers;

            var particleSystems = new List<string>();
            foreach (var particleSystemId in request.ParticleSystemIds)
            {
                if (_particleSystems.ContainsKey(particleSystemId))
                {
                    particleSystems.Add(particleSystemId);
                }
            }
            scene.ParticleSystems = particleSystems;

            var postProcessingEffects = new List<AdvancedGraphicsEnginePostProcessingEffect>();
            foreach (var effect in request.PostProcessingEffects)
            {
                postProcessingEffects.Add(effect);
            }
            scene.PostProcessingEffects = postProcessingEffects;

            _activeScenes[scene.SceneId] = scene;

            _logger.LogInformation("Advanced graphics scene created: {SceneId}", scene.SceneId);
            return Result.Success<AdvancedGraphicsEngineGraphicsScene>(scene);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating graphics scene {Name}", request.Name);
            return Result.Failure<AdvancedGraphicsEngineGraphicsScene>($"Failed to create scene: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedGraphicsEngineParticleSystem>> CreateParticleSystemAsync(AdvancedGraphicsEngineParticleSystemRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating particle system: {Name}", request.Name);

            var particleSystem = new AdvancedGraphicsEngineParticleSystem
            {
                SystemId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                EmitterSettings = request.EmitterSettings,
                AdvancedGraphicsEngineParticleSettings = request.AdvancedGraphicsEngineParticleSettings,
                BehaviorSettings = request.BehaviorSettings,
                AdvancedGraphicsEngineRenderSettings = request.AdvancedGraphicsEngineRenderSettings,
                IsActive = false,
                ParticleCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _particleSystems[particleSystem.SystemId] = particleSystem;

            _logger.LogInformation("Particle system created: {SystemId}", particleSystem.SystemId);
            return Result.Success<AdvancedGraphicsEngineParticleSystem>(particleSystem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating particle system {Name}", request.Name);
            return Result.Failure<AdvancedGraphicsEngineParticleSystem>($"Failed to create particle system: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedGraphicsEngineShaderProgram>> CompileShaderAsync(AdvancedGraphicsEngineShaderCompilationRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Compiling shader: {Name}", request.Name);

            // Simulate shader compilation (would integrate with graphics API)
            await Task.Delay(200, ct); // Simulate compilation time

            var shader = new AdvancedGraphicsEngineShaderProgram
            {
                ShaderId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                VertexShader = request.VertexShader,
                FragmentShader = request.FragmentShader,
                GeometryShader = request.GeometryShader,
                Uniforms = ParseShaderUniforms(request.VertexShader + request.FragmentShader),
                Attributes = ParseShaderAttributes(request.VertexShader),
                CompilationStatus = AdvancedGraphicsEngineShaderCompilationStatus.Success,
                CompiledAt = DateTime.UtcNow,
                PerformanceMetrics = new AdvancedGraphicsEngineShaderPerformanceMetrics
                {
                    EstimatedDrawCalls = 1000,
                    EstimatedFillRate = 0.8f,
                    EstimatedMemoryUsage = 256 * 1024 // 256KB
                }
            };

            _shaderPrograms[shader.ShaderId] = shader;

            _logger.LogInformation("Shader compiled successfully: {ShaderId}", shader.ShaderId);
            return Result.Success<AdvancedGraphicsEngineShaderProgram>(shader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compiling shader {Name}", request.Name);
            return Result.Failure<AdvancedGraphicsEngineShaderProgram>($"Failed to compile shader: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedGraphicsEngineLightingSetup>> CreateLightingSetupAsync(AdvancedGraphicsEngineLightingSetupRequest request, CancellationToken ct = default)
    {
        try
        {
            var setup = await _lightingEngine.CreateLightingSetupAsync(request, ct);
            return Result.Success(setup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lighting setup");
            return Result.Failure<AdvancedGraphicsEngineLightingSetup>($"Failed to create lighting setup: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedGraphicsEnginePostProcessingEffect>> CreatePostProcessingEffectAsync(AdvancedGraphicsEnginePostProcessingRequest request, CancellationToken ct = default)
    {
        try
        {
            var effect = await _postProcessingEngine.CreateEffectAsync(request, ct);
            return Result.Success(effect);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post-processing effect {Name}", request.Name);
            return Result.Failure<AdvancedGraphicsEnginePostProcessingEffect>($"Failed to create effect: {ex.Message}");
        }
    }

    public async Task<Result> RenderSceneAsync(string sceneId, AdvancedGraphicsEngineRenderContext context, CancellationToken ct = default)
    {
        try
        {
            if (!_activeScenes.TryGetValue(sceneId, out var scene))
            {
                return Result.Failure("Scene not found");
            }

            _logger.LogInformation("Rendering scene {SceneId} with context {Context}", sceneId, context.ContextId);

            // Background rendering
            foreach (var layer in scene.BackgroundLayers)
            {
                await RenderBackgroundLayerAsync(layer, context, ct);
            }

            // Lighting setup
            await _lightingEngine.ApplyLightingAsync(scene.AdvancedGraphicsEngineLightingSetup, context, ct);

            // Particle systems
            foreach (var particleSystemId in scene.ParticleSystems)
            {
                if (_particleSystems.TryGetValue(particleSystemId, out var particleSystem))
                {
                    await RenderParticleSystemAsync(particleSystem, context, ct);
                }
            }

            // Post-processing effects
            foreach (var effect in scene.PostProcessingEffects)
            {
                await _postProcessingEngine.ApplyEffectAsync(effect, context, ct);
            }

            _logger.LogInformation("Scene rendering completed: {SceneId}", sceneId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering scene {SceneId}", sceneId);
            return Result.Failure($"Failed to render scene: {ex.Message}");
        }
    }

    public async Task<Result<AdvancedGraphicsEngineRenderStatistics>> GetRenderStatisticsAsync(string sceneId, CancellationToken ct = default)
    {
        try
        {
            if (!_activeScenes.TryGetValue(sceneId, out var scene))
            {
                return Result.Failure<AdvancedGraphicsEngineRenderStatistics>("Scene not found");
            }

            var statistics = new AdvancedGraphicsEngineRenderStatistics
            {
                SceneId = sceneId,
                FrameRate = 60.0f, // Simulated
                DrawCalls = scene.BackgroundLayers.Count + scene.ParticleSystems.Count * 100,
                TriangleCount = CalculateTriangleCount(scene),
                TextureMemoryUsage = CalculateTextureMemory(scene),
                ShaderSwitches = scene.PostProcessingEffects.Count,
                ParticleCount = scene.ParticleSystems.Sum(id =>
                    _particleSystems.TryGetValue(id, out var ps) ? ps.ParticleCount : 0),
                LightingCalculations = scene.AdvancedGraphicsEngineLightingSetup.Lights.Count * 1000,
                PostProcessingTime = TimeSpan.FromMilliseconds(scene.PostProcessingEffects.Count * 2.5),
                TotalRenderTime = TimeSpan.FromMilliseconds(16.67) // ~60 FPS
            };

            return Result.Success<AdvancedGraphicsEngineRenderStatistics>(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting render statistics for {SceneId}", sceneId);
            return Result.Failure<AdvancedGraphicsEngineRenderStatistics>($"Failed to get statistics: {ex.Message}");
        }
    }

    #region Private Methods

    private void InitializeDefaultShaders()
    {
        // Initialize with common shader programs
        var defaultShaders = new[]
        {
            new AdvancedGraphicsEngineShaderProgram
            {
                ShaderId = "default_lighting",
                Name = "Default Lighting Shader",
                Description = "Standard lighting with ambient, diffuse, and specular components",
                CompilationStatus = AdvancedGraphicsEngineShaderCompilationStatus.Success,
                CompiledAt = DateTime.UtcNow
            },
            new AdvancedGraphicsEngineShaderProgram
            {
                ShaderId = "particle_system",
                Name = "Particle System Shader",
                Description = "Optimized shader for particle rendering",
                CompilationStatus = AdvancedGraphicsEngineShaderCompilationStatus.Success,
                CompiledAt = DateTime.UtcNow
            },
            new AdvancedGraphicsEngineShaderProgram
            {
                ShaderId = "post_process_bloom",
                Name = "Bloom Post-Processing",
                Description = "Bloom effect for bright highlights",
                CompilationStatus = AdvancedGraphicsEngineShaderCompilationStatus.Success,
                CompiledAt = DateTime.UtcNow
            }
        };

        foreach (var shader in defaultShaders)
        {
            _shaderPrograms[shader.ShaderId] = shader;
        }
    }

    private void InitializeDefaultParticleSystems()
    {
        // Initialize with common particle effects
        var defaultParticles = new[]
        {
            new AdvancedGraphicsEngineParticleSystem
            {
                SystemId = "fire_effect",
                Name = "Fire Effect",
                Description = "Realistic fire particle system",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            },
            new AdvancedGraphicsEngineParticleSystem
            {
                SystemId = "explosion_effect",
                Name = "Explosion Effect",
                Description = "Dramatic explosion with debris",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            },
            new AdvancedGraphicsEngineParticleSystem
            {
                SystemId = "magic_effect",
                Name = "Magic Effect",
                Description = "Magical particle effects",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var particle in defaultParticles)
        {
            _particleSystems[particle.SystemId] = particle;
        }
    }

    private async Task<AdvancedGraphicsEngineBackgroundLayer> CreateBackgroundLayerAsync(AdvancedGraphicsEngineBackgroundLayerRequest request, CancellationToken ct)
    {
        return new AdvancedGraphicsEngineBackgroundLayer
        {
            LayerId = Guid.NewGuid().ToString(),
            Name = request.Name,
            TexturePath = request.TexturePath,
            Position = request.Position,
            Scale = request.Scale,
            Opacity = request.Opacity,
            ScrollSpeed = request.ScrollSpeed,
            ParallaxFactor = request.ParallaxFactor,
            AdvancedGraphicsEngineBlendMode = request.AdvancedGraphicsEngineBlendMode,
            ShaderId = request.ShaderId
        };
    }

    private AdvancedGraphicsEngineCameraSettings CreateDefaultCameraSettings()
    {
        return new AdvancedGraphicsEngineCameraSettings
        {
            Position = new AdvancedGraphicsEngineGraphicsVector3(0, 0, -10),
            Target = new AdvancedGraphicsEngineGraphicsVector3(0, 0, 0),
            Up = new AdvancedGraphicsEngineGraphicsVector3(0, 1, 0),
            FieldOfView = 45.0f,
            NearPlane = 0.1f,
            FarPlane = 1000.0f,
            AdvancedGraphicsEngineProjectionMode = AdvancedGraphicsEngineProjectionMode.Perspective
        };
    }

    private AdvancedGraphicsEngineRenderSettings CreateDefaultRenderSettings()
    {
        return new AdvancedGraphicsEngineRenderSettings
        {
            AdvancedGraphicsEngineResolution = new AdvancedGraphicsEngineResolution(1920, 1080),
            AntiAliasing = AdvancedGraphicsEngineAntiAliasingMode.MSAA4x,
            AnisotropicFiltering = 16,
            AdvancedGraphicsEngineShadowQuality = AdvancedGraphicsEngineShadowQuality.High,
            AdvancedGraphicsEngineTextureQuality = AdvancedGraphicsEngineTextureQuality.High,
            AdvancedGraphicsEngineEffectQuality = AdvancedGraphicsEngineEffectQuality.High,
            VSync = true,
            TargetFrameRate = 60
        };
    }

    private IReadOnlyList<AdvancedGraphicsEngineShaderUniform> ParseShaderUniforms(string shaderCode)
    {
        // Simplified parsing - would use actual GLSL parser
        var uniforms = new List<AdvancedGraphicsEngineShaderUniform>();
        if (shaderCode.Contains("uniform"))
        {
            uniforms.Add(new AdvancedGraphicsEngineShaderUniform { Name = "u_time", Type = AdvancedGraphicsEngineUniformType.Float, Value = 0.0f });
            uniforms.Add(new AdvancedGraphicsEngineShaderUniform { Name = "u_resolution", Type = AdvancedGraphicsEngineUniformType.Vec2, Value = new AdvancedGraphicsEngineGraphicsVector2(1920, 1080) });
        }
        return uniforms;
    }

    private IReadOnlyList<AdvancedGraphicsEngineShaderAttribute> ParseShaderAttributes(string vertexShader)
    {
        // Simplified parsing
        var attributes = new List<AdvancedGraphicsEngineShaderAttribute>();
        if (vertexShader.Contains("attribute"))
        {
            attributes.Add(new AdvancedGraphicsEngineShaderAttribute { Name = "a_position", Type = AdvancedGraphicsEngineAttributeType.Vec3, Location = 0 });
            attributes.Add(new AdvancedGraphicsEngineShaderAttribute { Name = "a_texCoord", Type = AdvancedGraphicsEngineAttributeType.Vec2, Location = 1 });
        }
        return attributes;
    }

    private async Task RenderBackgroundLayerAsync(AdvancedGraphicsEngineBackgroundLayer layer, AdvancedGraphicsEngineRenderContext context, CancellationToken ct)
    {
        // Simulate background layer rendering
        await Task.Delay(5, ct);
    }

    private async Task RenderParticleSystemAsync(AdvancedGraphicsEngineParticleSystem particleSystem, AdvancedGraphicsEngineRenderContext context, CancellationToken ct)
    {
        // Simulate particle system rendering
        if (particleSystem.IsActive)
        {
            await Task.Delay(3, ct);
        }
    }

    private int CalculateTriangleCount(AdvancedGraphicsEngineGraphicsScene scene)
    {
        // Estimate triangle count based on scene complexity
        return scene.BackgroundLayers.Count * 1000 +
               scene.ParticleSystems.Count * 100 +
               scene.AdvancedGraphicsEngineLightingSetup.Lights.Count * 50;
    }

    private long CalculateTextureMemory(AdvancedGraphicsEngineGraphicsScene scene)
    {
        // Estimate texture memory usage (simplified)
        return scene.BackgroundLayers.Count * 4 * 1024 * 1024; // 4MB per layer
    }

    #endregion
}

/// <summary>
/// Lighting engine for dynamic lighting calculations.
/// </summary>
public class AdvancedGraphicsEngineLightingEngine
{
    private readonly ILogger<AdvancedGraphicsEngineLightingEngine> _logger;

    public AdvancedGraphicsEngineLightingEngine(ILogger<AdvancedGraphicsEngineLightingEngine> logger)
    {
        _logger = logger;
    }

    public async Task<AdvancedGraphicsEngineLightingSetup> CreateDefaultLightingAsync(AdvancedGraphicsEngineLightingPreset preset, CancellationToken ct = default)
    {
        var setup = new AdvancedGraphicsEngineLightingSetup
        {
            SetupId = Guid.NewGuid().ToString(),
            Name = $"{preset} Lighting",
            AdvancedGraphicsEngineAmbientLight = new AdvancedGraphicsEngineAmbientLight { Intensity = 0.3f, AdvancedGraphicsEngineColor = new AdvancedGraphicsEngineColor(1, 1, 1) },
            Lights = new List<AdvancedGraphicsEngineLightSource>(),
            ShadowsEnabled = true,
            AdvancedGraphicsEngineShadowQuality = AdvancedGraphicsEngineShadowQuality.High
        };

        var lights = new List<AdvancedGraphicsEngineLightSource>();

        switch (preset)
        {
            case AdvancedGraphicsEngineLightingPreset.Daylight:
                lights.Add(new AdvancedGraphicsEngineDirectionalLight
                {
                    Direction = new AdvancedGraphicsEngineGraphicsVector3(0.5f, -0.8f, 0.3f),
                    AdvancedGraphicsEngineColor = new AdvancedGraphicsEngineColor(1, 0.95f, 0.8f),
                    Intensity = 1.0f
                });
                break;

            case AdvancedGraphicsEngineLightingPreset.Night:
                setup.AdvancedGraphicsEngineAmbientLight = new AdvancedGraphicsEngineAmbientLight { Intensity = 0.1f, AdvancedGraphicsEngineColor = new AdvancedGraphicsEngineColor(0.2f, 0.2f, 0.4f) };
                lights.Add(new AdvancedGraphicsEngineDirectionalLight
                {
                    Direction = new AdvancedGraphicsEngineGraphicsVector3(0.2f, -0.8f, 0.5f),
                    AdvancedGraphicsEngineColor = new AdvancedGraphicsEngineColor(0.8f, 0.9f, 1.0f),
                    Intensity = 0.3f
                });
                break;

            case AdvancedGraphicsEngineLightingPreset.Arena:
                lights.AddRange(new AdvancedGraphicsEngineLightSource[]
                {
                    new AdvancedGraphicsEnginePointLight { Position = new Vector3(-5, 3, 0), AdvancedGraphicsEngineColor = new AdvancedGraphicsEngineColor(1, 0.8f, 0.6f), Intensity = 2.0f, Range = 10 },
                    new AdvancedGraphicsEnginePointLight { Position = new Vector3(5, 3, 0), AdvancedGraphicsEngineColor = new AdvancedGraphicsEngineColor(1, 0.8f, 0.6f), Intensity = 2.0f, Range = 10 },
                    new AdvancedGraphicsEngineSpotLight { Position = new Vector3(0, 8, 0), Direction = new Vector3(0, -1, 0), AdvancedGraphicsEngineColor = new AdvancedGraphicsEngineColor(1, 1, 1), Intensity = 1.5f, Angle = 45 }
                });
                break;
        }

        setup.Lights = lights;
        return setup;
    }

    public async Task<AdvancedGraphicsEngineLightingSetup> CreateLightingSetupAsync(AdvancedGraphicsEngineLightingSetupRequest request, CancellationToken ct = default)
    {
        var setup = new AdvancedGraphicsEngineLightingSetup
        {
            SetupId = Guid.NewGuid().ToString(),
            Name = request.Name,
            AdvancedGraphicsEngineAmbientLight = request.AdvancedGraphicsEngineAmbientLight,
            Lights = request.Lights,
            ShadowsEnabled = request.ShadowsEnabled,
            AdvancedGraphicsEngineShadowQuality = request.AdvancedGraphicsEngineShadowQuality
        };

        return setup;
    }

    public async Task ApplyLightingAsync(AdvancedGraphicsEngineLightingSetup setup, AdvancedGraphicsEngineRenderContext context, CancellationToken ct = default)
    {
        // Apply lighting calculations to render context
        await Task.Delay(2, ct);
    }
}

/// <summary>
/// Post-processing engine for visual effects.
/// </summary>
public class AdvancedGraphicsEnginePostProcessingEngine
{
    private readonly ILogger<AdvancedGraphicsEnginePostProcessingEngine> _logger;

    public AdvancedGraphicsEnginePostProcessingEngine(ILogger<AdvancedGraphicsEnginePostProcessingEngine> logger)
    {
        _logger = logger;
    }

    public async Task<AdvancedGraphicsEnginePostProcessingEffect> CreateEffectAsync(AdvancedGraphicsEnginePostProcessingRequest request, CancellationToken ct = default)
    {
        var effect = new AdvancedGraphicsEnginePostProcessingEffect
        {
            EffectId = Guid.NewGuid().ToString(),
            Name = request.Name,
            Type = request.Type,
            Parameters = request.Parameters,
            ShaderId = request.ShaderId,
            Priority = request.Priority,
            Enabled = true
        };

        return effect;
    }

    public async Task ApplyEffectAsync(AdvancedGraphicsEnginePostProcessingEffect effect, AdvancedGraphicsEngineRenderContext context, CancellationToken ct = default)
    {
        // Apply post-processing effect to render context
        await Task.Delay(1, ct);
    }
}

/// <summary>
/// Advanced Graphics Engine interface.
/// </summary>
public interface AdvancedGraphicsEngineIAdvancedGraphicsEngine
{
    Task<Result<AdvancedGraphicsEngineGraphicsScene>> CreateSceneAsync(AdvancedGraphicsEngineSceneCreationRequest request, CancellationToken ct = default);
    Task<Result<AdvancedGraphicsEngineParticleSystem>> CreateParticleSystemAsync(AdvancedGraphicsEngineParticleSystemRequest request, CancellationToken ct = default);
    Task<Result<AdvancedGraphicsEngineShaderProgram>> CompileShaderAsync(AdvancedGraphicsEngineShaderCompilationRequest request, CancellationToken ct = default);
    Task<Result<AdvancedGraphicsEngineLightingSetup>> CreateLightingSetupAsync(AdvancedGraphicsEngineLightingSetupRequest request, CancellationToken ct = default);
    Task<Result<AdvancedGraphicsEnginePostProcessingEffect>> CreatePostProcessingEffectAsync(AdvancedGraphicsEnginePostProcessingRequest request, CancellationToken ct = default);
    Task<Result> RenderSceneAsync(string sceneId, AdvancedGraphicsEngineRenderContext context, CancellationToken ct = default);
    Task<Result<AdvancedGraphicsEngineRenderStatistics>> GetRenderStatisticsAsync(string sceneId, CancellationToken ct = default);
}

/// <summary>
/// Graphics scene data.
/// </summary>
public class AdvancedGraphicsEngineGraphicsScene
{
    public string SceneId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public AdvancedGraphicsEngineResolution Dimensions { get; set; } = default!;
    public IReadOnlyList<AdvancedGraphicsEngineBackgroundLayer> BackgroundLayers { get; set; } = default!;
    public AdvancedGraphicsEngineLightingSetup AdvancedGraphicsEngineLightingSetup { get; set; } = default!;
    public IReadOnlyList<string> ParticleSystems { get; set; } = default!;
    public IReadOnlyList<AdvancedGraphicsEnginePostProcessingEffect> PostProcessingEffects { get; set; } = default!;
    public AdvancedGraphicsEngineCameraSettings AdvancedGraphicsEngineCameraSettings { get; set; } = default!;
    public AdvancedGraphicsEngineRenderSettings AdvancedGraphicsEngineRenderSettings { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Scene creation request.
/// </summary>
public class AdvancedGraphicsEngineSceneCreationRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public AdvancedGraphicsEngineResolution Dimensions { get; set; } = default!;
    public AdvancedGraphicsEngineLightingPreset AdvancedGraphicsEngineLightingPreset { get; set; } = default!;
    public IReadOnlyList<AdvancedGraphicsEngineBackgroundLayerRequest> BackgroundLayers { get; set; } = default!;
    public IReadOnlyList<string> ParticleSystemIds { get; set; } = default!;
    public IReadOnlyList<AdvancedGraphicsEnginePostProcessingEffect> PostProcessingEffects { get; set; } = default!;
}

/// <summary>
/// Background layer data.
/// </summary>
public class AdvancedGraphicsEngineBackgroundLayer
{
    public string LayerId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string TexturePath { get; set; } = default!;
    public Vector2 Position { get; set; } = default!;
    public Vector2 Scale { get; set; } = default!;
    public float Opacity { get; set; } = default!;
    public Vector2 ScrollSpeed { get; set; } = default!;
    public float ParallaxFactor { get; set; } = default!;
    public AdvancedGraphicsEngineBlendMode AdvancedGraphicsEngineBlendMode { get; set; } = default!;
    public string? ShaderId { get; set; } = default!;
}

/// <summary>
/// Background layer request.
/// </summary>
public class AdvancedGraphicsEngineBackgroundLayerRequest
{
    public string Name { get; set; } = default!;
    public string TexturePath { get; set; } = default!;
    public Vector2 Position { get; set; } = default!;
    public Vector2 Scale { get; set; } = default!;
    public float Opacity { get; set; } = default!;
    public Vector2 ScrollSpeed { get; set; } = default!;
    public float ParallaxFactor { get; set; } = default!;
    public AdvancedGraphicsEngineBlendMode AdvancedGraphicsEngineBlendMode { get; set; } = default!;
    public string? ShaderId { get; set; } = default!;
}

/// <summary>
/// Lighting setup data.
/// </summary>
public class AdvancedGraphicsEngineLightingSetup
{
    public string SetupId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public AdvancedGraphicsEngineAmbientLight AdvancedGraphicsEngineAmbientLight { get; set; } = default!;
    public IReadOnlyList<AdvancedGraphicsEngineLightSource> Lights { get; set; } = default!;
    public bool ShadowsEnabled { get; set; } = default!;
    public AdvancedGraphicsEngineShadowQuality AdvancedGraphicsEngineShadowQuality { get; set; } = default!;
}

/// <summary>
/// Lighting setup request.
/// </summary>
public class AdvancedGraphicsEngineLightingSetupRequest
{
    public string Name { get; set; } = default!;
    public AdvancedGraphicsEngineAmbientLight AdvancedGraphicsEngineAmbientLight { get; set; } = default!;
    public IReadOnlyList<AdvancedGraphicsEngineLightSource> Lights { get; set; } = default!;
    public bool ShadowsEnabled { get; set; } = default!;
    public AdvancedGraphicsEngineShadowQuality AdvancedGraphicsEngineShadowQuality { get; set; } = default!;
}

/// <summary>
/// Lighting preset enumeration.
/// </summary>
public enum AdvancedGraphicsEngineLightingPreset
{
    Daylight,
    Night,
    Arena,
    Underground,
    Custom
}

/// <summary>
/// Particle system data.
/// </summary>
public class AdvancedGraphicsEngineParticleSystem
{
    public string SystemId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public AdvancedGraphicsEngineParticleEmitterSettings EmitterSettings { get; set; } = default!;
    public AdvancedGraphicsEngineParticleSettings AdvancedGraphicsEngineParticleSettings { get; set; } = default!;
    public AdvancedGraphicsEngineParticleBehaviorSettings BehaviorSettings { get; set; } = default!;
    public AdvancedGraphicsEngineParticleRenderSettings AdvancedGraphicsEngineRenderSettings { get; set; } = default!;
    public bool IsActive { get; set; } = default!;
    public int ParticleCount { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Particle system request.
/// </summary>
public class AdvancedGraphicsEngineParticleSystemRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public AdvancedGraphicsEngineParticleEmitterSettings EmitterSettings { get; set; } = default!;
    public AdvancedGraphicsEngineParticleSettings AdvancedGraphicsEngineParticleSettings { get; set; } = default!;
    public AdvancedGraphicsEngineParticleBehaviorSettings BehaviorSettings { get; set; } = default!;
    public AdvancedGraphicsEngineParticleRenderSettings AdvancedGraphicsEngineRenderSettings { get; set; } = default!;
}

/// <summary>
/// Particle emitter settings.
/// </summary>
public class AdvancedGraphicsEngineParticleEmitterSettings
{
    public Vector3 Position { get; set; } = default!;
    public Vector3 Direction { get; set; } = default!;
    public float Spread { get; set; } = default!;
    public float Rate { get; set; } = default!;
    public float Duration { get; set; } = default!;
    public int MaxParticles { get; set; } = default!;
}

/// <summary>
/// Particle settings.
/// </summary>
public class AdvancedGraphicsEngineParticleSettings
{
    public Vector2 Size { get; set; } = default!;
    public AdvancedGraphicsEngineColor StartColor { get; set; } = default!;
    public AdvancedGraphicsEngineColor EndColor { get; set; } = default!;
    public float StartAlpha { get; set; } = default!;
    public float EndAlpha { get; set; } = default!;
    public float Lifetime { get; set; } = default!;
}

/// <summary>
/// Particle behavior settings.
/// </summary>
public class AdvancedGraphicsEngineParticleBehaviorSettings
{
    public Vector3 Gravity { get; set; } = default!;
    public Vector3 Wind { get; set; } = default!;
    public float Drag { get; set; } = default!;
    public bool CollidesWithWorld { get; set; } = default!;
    public bool AffectedByLighting { get; set; } = default!;
}

/// <summary>
/// Particle render settings.
/// </summary>
public class AdvancedGraphicsEngineParticleRenderSettings
{
    public string TexturePath { get; set; } = default!;
    public AdvancedGraphicsEngineBlendMode AdvancedGraphicsEngineBlendMode { get; set; } = default!;
    public bool SoftParticles { get; set; } = default!;
    public bool SortByDepth { get; set; } = default!;
}

/// <summary>
/// Shader program data.
/// </summary>
public class AdvancedGraphicsEngineShaderProgram
{
    public string ShaderId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string VertexShader { get; set; } = default!;
    public string FragmentShader { get; set; } = default!;
    public string? GeometryShader { get; set; } = default!;
    public IReadOnlyList<AdvancedGraphicsEngineShaderUniform> Uniforms { get; set; } = default!;
    public IReadOnlyList<AdvancedGraphicsEngineShaderAttribute> Attributes { get; set; } = default!;
    public AdvancedGraphicsEngineShaderCompilationStatus CompilationStatus { get; set; } = default!;
    public DateTime CompiledAt { get; set; } = default!;
    public AdvancedGraphicsEngineShaderPerformanceMetrics PerformanceMetrics { get; set; } = default!;
}

/// <summary>
/// Shader compilation request.
/// </summary>
public class AdvancedGraphicsEngineShaderCompilationRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string VertexShader { get; set; } = default!;
    public string FragmentShader { get; set; } = default!;
    public string? GeometryShader { get; set; } = default!;
}

/// <summary>
/// Shader compilation status.
/// </summary>
public enum AdvancedGraphicsEngineShaderCompilationStatus
{
    Pending,
    Success,
    Failed
}

/// <summary>
/// Shader uniform data.
/// </summary>
public class AdvancedGraphicsEngineShaderUniform
{
    public string Name { get; set; } = default!;
    public AdvancedGraphicsEngineUniformType Type { get; set; } = default!;
    public object Value { get; set; } = default!;
}

/// <summary>
/// Shader attribute data.
/// </summary>
public class AdvancedGraphicsEngineShaderAttribute
{
    public string Name { get; set; } = default!;
    public AdvancedGraphicsEngineAttributeType Type { get; set; } = default!;
    public int Location { get; set; } = default!;
}

/// <summary>
/// Shader performance metrics.
/// </summary>
public class AdvancedGraphicsEngineShaderPerformanceMetrics
{
    public int EstimatedDrawCalls { get; set; } = default!;
    public float EstimatedFillRate { get; set; } = default!;
    public long EstimatedMemoryUsage { get; set; } = default!;
}

/// <summary>
/// Uniform type enumeration.
/// </summary>
public enum AdvancedGraphicsEngineUniformType
{
    Float,
    Vec2,
    Vec3,
    Vec4,
    Mat4,
    Texture2D,
    Bool
}

/// <summary>
/// Attribute type enumeration.
/// </summary>
public enum AdvancedGraphicsEngineAttributeType
{
    Float,
    Vec2,
    Vec3,
    Vec4
}

/// <summary>
/// Post-processing effect data.
/// </summary>
public class AdvancedGraphicsEnginePostProcessingEffect
{
    public string EffectId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public AdvancedGraphicsEnginePostProcessingType Type { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public string? ShaderId { get; set; } = default!;
    public int Priority { get; set; } = default!;
    public bool Enabled { get; set; } = default!;
}

/// <summary>
/// Post-processing request.
/// </summary>
public class AdvancedGraphicsEnginePostProcessingRequest
{
    public string Name { get; set; } = default!;
    public AdvancedGraphicsEnginePostProcessingType Type { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public string? ShaderId { get; set; } = default!;
    public int Priority { get; set; } = default!;
}

/// <summary>
/// Post-processing type enumeration.
/// </summary>
public enum AdvancedGraphicsEnginePostProcessingType
{
    Bloom,
    ToneMapping,
    ColorGrading,
    MotionBlur,
    DepthOfField,
    Vignette,
    ChromaticAberration,
    FilmGrain
}

/// <summary>
/// Camera settings.
/// </summary>
public class AdvancedGraphicsEngineCameraSettings
{
    public Vector3 Position { get; set; } = default!;
    public Vector3 Target { get; set; } = default!;
    public Vector3 Up { get; set; } = default!;
    public float FieldOfView { get; set; } = default!;
    public float NearPlane { get; set; } = default!;
    public float FarPlane { get; set; } = default!;
    public AdvancedGraphicsEngineProjectionMode AdvancedGraphicsEngineProjectionMode { get; set; } = default!;
}

/// <summary>
/// Render settings.
/// </summary>
public class AdvancedGraphicsEngineRenderSettings
{
    public AdvancedGraphicsEngineResolution AdvancedGraphicsEngineResolution { get; set; } = default!;
    public AdvancedGraphicsEngineAntiAliasingMode AntiAliasing { get; set; } = default!;
    public int AnisotropicFiltering { get; set; } = default!;
    public AdvancedGraphicsEngineShadowQuality AdvancedGraphicsEngineShadowQuality { get; set; } = default!;
    public AdvancedGraphicsEngineTextureQuality AdvancedGraphicsEngineTextureQuality { get; set; } = default!;
    public AdvancedGraphicsEngineEffectQuality AdvancedGraphicsEngineEffectQuality { get; set; } = default!;
    public bool VSync { get; set; } = default!;
    public int TargetFrameRate { get; set; } = default!;
}

/// <summary>
/// Render context.
/// </summary>
public class AdvancedGraphicsEngineRenderContext
{
    public string ContextId { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public IReadOnlyDictionary<string, object> RenderState { get; set; } = default!;
}

/// <summary>
/// Render statistics.
/// </summary>
public class AdvancedGraphicsEngineRenderStatistics
{
    public string SceneId { get; set; } = default!;
    public float FrameRate { get; set; } = default!;
    public int DrawCalls { get; set; } = default!;
    public int TriangleCount { get; set; } = default!;
    public long TextureMemoryUsage { get; set; } = default!;
    public int ShaderSwitches { get; set; } = default!;
    public int ParticleCount { get; set; } = default!;
    public int LightingCalculations { get; set; } = default!;
    public TimeSpan PostProcessingTime { get; set; } = default!;
    public TimeSpan TotalRenderTime { get; set; } = default!;
}

/// <summary>
/// Light source base class.
/// </summary>
public abstract record AdvancedGraphicsEngineLightSource(
    string LightId,
    AdvancedGraphicsEngineLightType Type,
    AdvancedGraphicsEngineColor AdvancedGraphicsEngineColor,
    float Intensity);

/// <summary>
/// Ambient light.
/// </summary>
public record AdvancedGraphicsEngineAmbientLight(float Intensity = default, AdvancedGraphicsEngineColor AdvancedGraphicsEngineColor = default) : AdvancedGraphicsEngineLightSource("ambient", AdvancedGraphicsEngineLightType.Ambient, AdvancedGraphicsEngineColor, Intensity);

/// <summary>
/// Directional light.
/// </summary>
public record AdvancedGraphicsEngineDirectionalLight(Vector3 Direction = default, AdvancedGraphicsEngineColor AdvancedGraphicsEngineColor = default, float Intensity = default) : AdvancedGraphicsEngineLightSource(Guid.NewGuid().ToString(), AdvancedGraphicsEngineLightType.Directional, AdvancedGraphicsEngineColor, Intensity);

/// <summary>
/// Point light.
/// </summary>
public record AdvancedGraphicsEnginePointLight(Vector3 Position = default, AdvancedGraphicsEngineColor AdvancedGraphicsEngineColor = default, float Intensity = default, float Range = default) : AdvancedGraphicsEngineLightSource(Guid.NewGuid().ToString(), AdvancedGraphicsEngineLightType.Point, AdvancedGraphicsEngineColor, Intensity);

/// <summary>
/// Spot light.
/// </summary>
public record AdvancedGraphicsEngineSpotLight(Vector3 Position = default, Vector3 Direction = default, AdvancedGraphicsEngineColor AdvancedGraphicsEngineColor = default, float Intensity = default, float Angle = default) : AdvancedGraphicsEngineLightSource(Guid.NewGuid().ToString(), AdvancedGraphicsEngineLightType.Spot, AdvancedGraphicsEngineColor, Intensity);

/// <summary>
/// Light type enumeration.
/// </summary>
public enum AdvancedGraphicsEngineLightType
{
    Ambient,
    Directional,
    Point,
    Spot
}

/// <summary>
/// Blend mode enumeration.
/// </summary>
public enum AdvancedGraphicsEngineBlendMode
{
    Normal,
    Additive,
    Multiply,
    Screen,
    Overlay
}

/// <summary>
/// Projection mode enumeration.
/// </summary>
public enum AdvancedGraphicsEngineProjectionMode
{
    Perspective,
    Orthographic
}

/// <summary>
/// Anti-aliasing mode enumeration.
/// </summary>
public enum AdvancedGraphicsEngineAntiAliasingMode
{
    None,
    FXAA,
    MSAA2x,
    MSAA4x,
    MSAA8x
}

/// <summary>
/// Shadow quality enumeration.
/// </summary>
public enum AdvancedGraphicsEngineShadowQuality
{
    Off,
    Low,
    Medium,
    High,
    Ultra
}

/// <summary>
/// Texture quality enumeration.
/// </summary>
public enum AdvancedGraphicsEngineTextureQuality
{
    Low,
    Medium,
    High,
    Ultra
}

/// <summary>
/// Effect quality enumeration.
/// </summary>
public enum AdvancedGraphicsEngineEffectQuality
{
    Low,
    Medium,
    High,
    Ultra
}

/// <summary>
/// AdvancedGraphicsEngineResolution data.
/// </summary>
public class AdvancedGraphicsEngineResolution
{
    public AdvancedGraphicsEngineResolution() { }
    public AdvancedGraphicsEngineResolution(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; set; } = default!;
    public int Height { get; set; } = default!;
}

/// <summary>
/// Vector2 data.
/// </summary>
public class AdvancedGraphicsEngineGraphicsVector2
{
    public AdvancedGraphicsEngineGraphicsVector2() { }
    public AdvancedGraphicsEngineGraphicsVector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; set; } = default!;
    public float Y { get; set; } = default!;
}

/// <summary>
/// Vector3 data.
/// </summary>
public class AdvancedGraphicsEngineGraphicsVector3
{
    public AdvancedGraphicsEngineGraphicsVector3() { }
    public AdvancedGraphicsEngineGraphicsVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public float X { get; set; } = default!;
    public float Y { get; set; } = default!;
    public float Z { get; set; } = default!;

    // Implicit conversion to the shared Vector3 (double components)
    public static implicit operator SaveState.Application.Mugen.Vector3(AdvancedGraphicsEngineGraphicsVector3 v)
        => new SaveState.Application.Mugen.Vector3(v.X, v.Y, v.Z);

    // Implicit conversion from shared Vector3 to this type
    public static implicit operator AdvancedGraphicsEngineGraphicsVector3(SaveState.Application.Mugen.Vector3 v)
        => new AdvancedGraphicsEngineGraphicsVector3((float)v.X, (float)v.Y, (float)v.Z);
}

/// <summary>
/// AdvancedGraphicsEngineColor data.
/// </summary>
public class AdvancedGraphicsEngineColor
{
    public AdvancedGraphicsEngineColor() { }
    public AdvancedGraphicsEngineColor(float r, float g, float b, float a = 1.0f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public float R { get; set; } = default!;
    public float G { get; set; } = default!;
    public float B { get; set; } = default!;
    public float A { get; set; } = default!;
}
