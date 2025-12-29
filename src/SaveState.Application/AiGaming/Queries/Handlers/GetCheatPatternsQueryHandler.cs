using MediatR;
using SaveState.Application.AiGaming.DTOs;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.Common.Enums;

namespace SaveState.Application.AiGaming.Queries.Handlers;

public class GetCheatPatternsQueryHandler : IRequestHandler<GetCheatPatternsQuery, Result<IReadOnlyList<CheatPatternDto>>>
{
    public async Task<Result<IReadOnlyList<CheatPatternDto>>> Handle(GetCheatPatternsQuery request, CancellationToken ct)
    {
        // Mock implementation - in a real system, this would query a database of known cheat patterns
        var patterns = new List<CheatPatternDto>();

        // Add some mock patterns
        if (string.IsNullOrEmpty(request.GameTitle) || request.GameTitle.Contains("Test", StringComparison.OrdinalIgnoreCase))
        {
            patterns.Add(new CheatPatternDto
            {
                PatternId = "speed_hack_001",
                GameTitle = "Test Game",
                Type = CheatType.SpeedHack,
                Description = "Detects when game speed is modified",
                AffectedAddresses = new[] { 0x1000L, 0x2000L },
                DetectionThreshold = 0.8,
                LastDetected = DateTime.UtcNow.AddDays(-1),
                DetectionCount = 15
            });

            patterns.Add(new CheatPatternDto
            {
                PatternId = "infinite_ammo_001",
                GameTitle = "Test Game",
                Type = CheatType.InfiniteAmmo,
                Description = "Detects infinite ammunition cheats",
                AffectedAddresses = new[] { 0x3000L, 0x4000L },
                DetectionThreshold = 0.9,
                LastDetected = DateTime.UtcNow.AddHours(-2),
                DetectionCount = 8
            });
        }

        // Filter by type if specified
        if (request.Type.HasValue)
        {
            patterns = patterns.Where(p => p.Type == request.Type.Value).ToList();
        }

        return Result<IReadOnlyList<CheatPatternDto>>.Success(patterns);
    }
}
