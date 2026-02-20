using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.Graphics.Managers;

/// <summary>
/// Manages particle systems and their lifecycle.
/// </summary>
public sealed class ParticleManager
{
    private readonly ILogger<ParticleManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, ParticleSystem> _particleSystems = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ParticleManager"/> class.
    /// </summary>
    public ParticleManager(ILogger<ParticleManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        InitializeDefaultParticleSystems();
    }

    /// <summary>
    /// Creates a particle system.
    /// </summary>
    public Task<Result<ParticleSystem>> CreateParticleSystemAsync(ParticleSystemRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating particle system: {Name}", request.Name);

            var particleSystem = new ParticleSystem
            {
                SystemId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                EmitterSettings = request.EmitterSettings,
                ParticleSettings = request.ParticleSettings,
                BehaviorSettings = request.BehaviorSettings,
                RenderSettings = request.RenderSettings,
                IsActive = false,
                ParticleCount = 0,
                CreatedAt = _timeProvider.UtcNow
            };

            _particleSystems[particleSystem.SystemId] = particleSystem;

            _logger.LogInformation("Particle system created: {SystemId}", particleSystem.SystemId);
            return Task.FromResult(Result<ParticleSystem>.Success(particleSystem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating particle system {Name}", request.Name);
            return Task.FromResult(Result<ParticleSystem>.Failure($"Failed to create particle system: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets a particle system by ID.
    /// </summary>
    public Task<Result<ParticleSystem>> GetParticleSystemAsync(string systemId, CancellationToken ct = default)
    {
        if (_particleSystems.TryGetValue(systemId, out var system))
        {
            return Task.FromResult(Result<ParticleSystem>.Success(system));
        }

        return Task.FromResult(Result<ParticleSystem>.Failure("Particle system not found"));
    }

    /// <summary>
    /// Activates a particle system.
    /// </summary>
    public Task ActivateAsync(string systemId, CancellationToken ct = default)
    {
        if (_particleSystems.TryGetValue(systemId, out var system))
        {
            system.IsActive = true;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Deactivates a particle system.
    /// </summary>
    public Task DeactivateAsync(string systemId, CancellationToken ct = default)
    {
        if (_particleSystems.TryGetValue(systemId, out var system))
        {
            system.IsActive = false;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Renders a particle system.
    /// </summary>
    public async Task RenderAsync(ParticleSystem particleSystem, RenderContext context, CancellationToken ct = default)
    {
        if (particleSystem.IsActive)
        {
            await Task.Delay(3, ct);
        }
    }

    /// <summary>
    /// Gets all registered particle systems.
    /// </summary>
    public IReadOnlyDictionary<string, ParticleSystem> GetAllSystems() => _particleSystems;

    private void InitializeDefaultParticleSystems()
    {
        var defaultParticles = new[]
        {
            new ParticleSystem
            {
                SystemId = "fire_effect",
                Name = "Fire Effect",
                Description = "Realistic fire particle system",
                IsActive = false,
                CreatedAt = _timeProvider.UtcNow
            },
            new ParticleSystem
            {
                SystemId = "explosion_effect",
                Name = "Explosion Effect",
                Description = "Dramatic explosion with debris",
                IsActive = false,
                CreatedAt = _timeProvider.UtcNow
            },
            new ParticleSystem
            {
                SystemId = "magic_effect",
                Name = "Magic Effect",
                Description = "Magical particle effects",
                IsActive = false,
                CreatedAt = _timeProvider.UtcNow
            }
        };

        foreach (var particle in defaultParticles)
        {
            _particleSystems[particle.SystemId] = particle;
        }
    }
}

// Particle system models
public class ParticleSystem
{
    public string SystemId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ParticleEmitterSettings EmitterSettings { get; set; } = default!;
    public ParticleSettings ParticleSettings { get; set; } = default!;
    public ParticleBehaviorSettings BehaviorSettings { get; set; } = default!;
    public ParticleRenderSettings RenderSettings { get; set; } = default!;
    public bool IsActive { get; set; }
    public int ParticleCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ParticleSystemRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ParticleEmitterSettings EmitterSettings { get; set; } = default!;
    public ParticleSettings ParticleSettings { get; set; } = default!;
    public ParticleBehaviorSettings BehaviorSettings { get; set; } = default!;
    public ParticleRenderSettings RenderSettings { get; set; } = default!;
}

public class ParticleEmitterSettings
{
    public GraphicsVector3 Position { get; set; } = default!;
    public GraphicsVector3 Direction { get; set; } = default!;
    public float Spread { get; set; }
    public float Rate { get; set; }
    public float Duration { get; set; }
    public int MaxParticles { get; set; }
}

public class ParticleSettings
{
    public GraphicsVector2 Size { get; set; } = default!;
    public GraphicsColor StartColor { get; set; } = default!;
    public GraphicsColor EndColor { get; set; } = default!;
    public float StartAlpha { get; set; }
    public float EndAlpha { get; set; }
    public float Lifetime { get; set; }
}

public class ParticleBehaviorSettings
{
    public GraphicsVector3 Gravity { get; set; } = default!;
    public GraphicsVector3 Wind { get; set; } = default!;
    public float Drag { get; set; }
    public bool CollidesWithWorld { get; set; }
    public bool AffectedByLighting { get; set; }
}

public class ParticleRenderSettings
{
    public string TexturePath { get; set; } = default!;
    public BlendMode BlendMode { get; set; }
    public bool SoftParticles { get; set; }
    public bool SortByDepth { get; set; }
}

public enum BlendMode { Normal, Additive, Multiply, Screen, Overlay }
