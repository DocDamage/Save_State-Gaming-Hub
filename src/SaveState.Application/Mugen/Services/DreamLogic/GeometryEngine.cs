using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.DreamLogic;
using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.DreamLogic;

/// <summary>
/// Engine for generating and manipulating impossible geometries.
/// </summary>
public class GeometryEngine
{
    private readonly ILogger<GeometryEngine> _logger;

    public GeometryEngine(ILogger<GeometryEngine> logger)
    {
        _logger = logger;
    }

    public Task<DreamArena> GenerateArenaAsync(DreamArenaRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating dream arena: {ArenaName}", request.ArenaName);

        var arena = new DreamArena
        {
            ArenaId = Guid.NewGuid().ToString(),
            Name = request.ArenaName,
            DreamTheme = request.DreamTheme,
            BaseGeometry = GenerateBaseGeometry(request.Dimensions),
            DreamPotential = CalculateDreamPotential(request.DreamTheme),
            EmotionalResonance = 0.5f,
            CreatedAt = DateTime.UtcNow,
            StabilityRating = 1.0f
        };

        return Task.FromResult(arena);
    }

    public Task<ImpossibleGeometry> ApplyTransformationAsync(DreamState state, GeometryTransformationRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Applying {TransformationType} geometry transformation", request.TransformationType);

        var geometry = new ImpossibleGeometry
        {
            TransformationId = Guid.NewGuid().ToString(),
            GeometryType = request.TransformationType,
            AffectedArea = request.AffectedArea,
            TransformationParameters = request.Parameters,
            ResultingGeometry = TransformGeometry(state.CurrentGeometry, request),
            StabilityChange = -0.1f,
            AppliedAt = DateTime.UtcNow
        };

        return Task.FromResult(geometry);
    }

    private ArenaGeometry GenerateBaseGeometry(System.Numerics.Vector3 dimensions)
    {
        return new ArenaGeometry
        {
            Dimensions = dimensions,
            GravityDirection = new System.Numerics.Vector3(0f, -1f, 0f),
            SurfaceType = SurfaceType.Solid,
            Boundaries = new List<Boundary>()
        };
    }

    private ArenaGeometry TransformGeometry(ArenaGeometry current, GeometryTransformationRequest request)
    {
        return new ArenaGeometry
        {
            Dimensions = current.Dimensions,
            GravityDirection = request.TransformationType == GeometryType.Warped
                ? new System.Numerics.Vector3(0f, 1f, 0f)
                : current.GravityDirection,
            SurfaceType = current.SurfaceType,
            Boundaries = current.Boundaries
        };
    }

    private float CalculateDreamPotential(DreamTheme theme)
    {
        return theme switch
        {
            DreamTheme.Surreal => 0.9f,
            DreamTheme.Nightmare => 0.8f,
            DreamTheme.Fantasy => 0.7f,
            DreamTheme.Memory => 0.6f,
            DreamTheme.Collective => 0.85f,
            _ => 0.5f
        };
    }
}

/// <summary>
/// Legacy alias for backward compatibility.
/// </summary>
public class DreamLogicArenaServiceGeometryEngine : GeometryEngine
{
    public DreamLogicArenaServiceGeometryEngine(ILogger<GeometryEngine> logger) : base(logger) { }
}
