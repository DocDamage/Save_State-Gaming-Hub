using System.Numerics;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.ProceduralContentGeneration;

/// <summary>
/// Stage generator for procedural stage creation.
/// </summary>
public class ProceduralContentGeneratorStageGenerator
{
    private readonly ILogger<ProceduralContentGeneratorStageGenerator> _logger;

    public ProceduralContentGeneratorStageGenerator(ILogger<ProceduralContentGeneratorStageGenerator> logger)
    {
        _logger = logger;
    }

    public async Task<ProceduralContentGeneratorStageLayout> GenerateLayoutAsync(ProceduralContentGeneratorStageGenerationRequest request, CancellationToken ct)
    {
        return new ProceduralContentGeneratorStageLayout
        {
            Width = (int)request.Dimensions.Size.X,
            Height = (int)request.Dimensions.Size.Y,
            Platforms = GeneratePlatforms(request),
            BackgroundLayers = GenerateBackgroundLayers(request),
            CameraBounds = new ProceduralContentGeneratorRectangle(0, 0, (int)request.Dimensions.Size.X, (int)request.Dimensions.Size.Y),
            SpawnPoints = new List<Vector2> { new Vector2(100, 200), new Vector2(900, 200) }
        };
    }

    private IReadOnlyList<ProceduralContentGeneratorPlatform> GeneratePlatforms(ProceduralContentGeneratorStageGenerationRequest request)
    {
        return new List<ProceduralContentGeneratorPlatform>
        {
            new ProceduralContentGeneratorPlatform { Position = new ProceduralContentGeneratorProceduralVector2(0, 250), Width = (int)request.Dimensions.Size.X, Height = 20, Type = "Ground" },
            new ProceduralContentGeneratorPlatform { Position = new ProceduralContentGeneratorProceduralVector2(200, 150), Width = 150, Height = 15, Type = "Floating" },
            new ProceduralContentGeneratorPlatform { Position = new ProceduralContentGeneratorProceduralVector2(650, 150), Width = 150, Height = 15, Type = "Floating" }
        };
    }

    private IReadOnlyList<ProceduralContentGeneratorBackgroundLayer> GenerateBackgroundLayers(ProceduralContentGeneratorStageGenerationRequest request)
    {
        return new[]
        {
            new ProceduralContentGeneratorBackgroundLayer { Image = "background_layer1.png", ParallaxFactor = 0.2f, Depth = 3 },
            new ProceduralContentGeneratorBackgroundLayer { Image = "background_layer2.png", ParallaxFactor = 0.5f, Depth = 2 },
            new ProceduralContentGeneratorBackgroundLayer { Image = "background_layer3.png", ParallaxFactor = 1.0f, Depth = 1 }
        };
    }
}
