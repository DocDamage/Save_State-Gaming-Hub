using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.NarrativeMemory;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.NarrativeMemory.Engines;

/// <summary>
/// Engine for synthesizing moves from crystals.
/// </summary>
public class SynthesisEngine
{
    private readonly ILogger<SynthesisEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, SynthesizedMove> _synthesizedMoves;

    /// <summary>
    /// Initializes a new instance of the <see cref="SynthesisEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider.</param>
    public SynthesisEngine(ILogger<SynthesisEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _synthesizedMoves = new ConcurrentDictionary<string, SynthesizedMove>();
    }

    /// <summary>
    /// Synthesizes a new move from provided crystals.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The move synthesis request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The synthesized move.</returns>
    public Task<SynthesizedMove> SynthesizeMoveAsync(
        string userId,
        MoveSynthesisRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Synthesizing move for user {UserId} using {CrystalCount} crystals, desired type: {MoveType}",
            userId,
            request.CrystalIds.Count,
            request.DesiredMoveType);

        var moveId = Guid.NewGuid().ToString();
        var now = _timeProvider.UtcNow;

        // Calculate move properties based on input crystals
        var rarity = CalculateMoveRarity(request.CrystalIds.Count);
        var power = CalculateMovePower(request);
        var effects = GenerateMoveEffects(request);
        var stability = CalculateStability(request);

        var synthesizedMove = new SynthesizedMove
        {
            MoveId = moveId,
            Name = GenerateMoveName(request, rarity),
            Power = power,
            Effects = effects,
            Rarity = rarity,
            SourceCrystals = request.CrystalIds.ToList(),
            SynthesizedAt = now,
            Stability = stability
        };

        _synthesizedMoves[moveId] = synthesizedMove;

        _logger.LogInformation(
            "Successfully synthesized {Rarity} move {MoveName} (ID: {MoveId}) with power {Power} and stability {Stability:P}",
            rarity,
            synthesizedMove.Name,
            moveId,
            power,
            stability);

        return Task.FromResult(synthesizedMove);
    }

    /// <summary>
    /// Gets a synthesized move by ID.
    /// </summary>
    /// <param name="moveId">The move ID.</param>
    /// <returns>The synthesized move if found; otherwise, null.</returns>
    public SynthesizedMove? GetMove(string moveId)
    {
        _synthesizedMoves.TryGetValue(moveId, out var move);
        return move;
    }

    /// <summary>
    /// Gets all synthesized moves for a player based on the source crystals.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <returns>The collection of synthesized moves.</returns>
    public IEnumerable<SynthesizedMove> GetPlayerMoves(string playerId)
    {
        // Note: Since SynthesizedMove doesn't have a direct PlayerId property,
        // we would need to look up the source crystals to determine ownership.
        // For now, return all moves (in a real implementation, this would query the crystal ownership)
        return _synthesizedMoves.Values;
    }

    /// <summary>
    /// Removes a synthesized move.
    /// </summary>
    /// <param name="moveId">The move ID.</param>
    /// <returns>True if removed; otherwise, false.</returns>
    public bool RemoveMove(string moveId)
    {
        return _synthesizedMoves.TryRemove(moveId, out _);
    }

    /// <summary>
    /// Gets all synthesized moves.
    /// </summary>
    /// <returns>The collection of all synthesized moves.</returns>
    public IEnumerable<SynthesizedMove> GetAllMoves()
    {
        return _synthesizedMoves.Values;
    }

    private static CrystalRarity CalculateMoveRarity(int crystalCount)
    {
        // More crystals = higher chance of better rarity
        var baseScore = crystalCount * 15;
        var randomFactor = Random.Shared.Next(0, 30);
        var totalScore = baseScore + randomFactor;

        return totalScore switch
        {
            >= 80 => CrystalRarity.Legendary,
            >= 60 => CrystalRarity.Epic,
            >= 40 => CrystalRarity.Rare,
            >= 20 => CrystalRarity.Uncommon,
            _ => CrystalRarity.Common
        };
    }

    private static int CalculateMovePower(MoveSynthesisRequest request)
    {
        // Base power depends on number of crystals
        var basePower = request.CrystalIds.Count * 25;

        // Move type affects power
        var typeMultiplier = request.DesiredMoveType.ToLowerInvariant() switch
        {
            "ultimate" => 2.0f,
            "special" => 1.5f,
            "combo" => 1.2f,
            "basic" => 1.0f,
            _ => 1.0f
        };

        // Add some randomness
        var randomFactor = 0.9f + (Random.Shared.NextSingle() * 0.2f);

        return (int)(basePower * typeMultiplier * randomFactor);
    }

    private static List<string> GenerateMoveEffects(MoveSynthesisRequest request)
    {
        var effects = new List<string>();

        // Add base effect based on move type
        effects.Add(request.DesiredMoveType.ToLowerInvariant() switch
        {
            "ultimate" => "Massive damage to all enemies",
            "special" => "High damage with special property",
            "combo" => "Multi-hit attack",
            "defensive" => "Blocks incoming damage",
            "buff" => "Increases stats temporarily",
            "debuff" => "Reduces enemy stats",
            _ => "Standard attack"
        });

        // Add additional effects based on crystal count
        if (request.CrystalIds.Count >= 3)
        {
            effects.Add("Critical hit chance increased");
        }

        if (request.CrystalIds.Count >= 5)
        {
            effects.Add("Elemental damage bonus");
        }

        if (request.CrystalIds.Count >= 7)
        {
            effects.Add("Stuns enemy on hit");
        }

        return effects;
    }

    private static float CalculateStability(MoveSynthesisRequest request)
    {
        // More crystals = more stable move
        var baseStability = Math.Min(0.3f + (request.CrystalIds.Count * 0.1f), 0.95f);

        // Certain move types are more stable
        var typeStability = request.DesiredMoveType.ToLowerInvariant() switch
        {
            "basic" => 1.0f,
            "combo" => 0.95f,
            "special" => 0.85f,
            "ultimate" => 0.7f,
            _ => 0.9f
        };

        return baseStability * typeStability;
    }

    private static string GenerateMoveName(MoveSynthesisRequest request, CrystalRarity rarity)
    {
        var prefixes = rarity switch
        {
            CrystalRarity.Legendary => new[] { "Celestial", "Divine", "Eternal", "Cosmic" },
            CrystalRarity.Epic => new[] { "Mystic", "Arcane", "Primal", "Ancient" },
            CrystalRarity.Rare => new[] { "Enhanced", "Refined", "Advanced", "Superior" },
            CrystalRarity.Uncommon => new[] { "Improved", "Better", "Skilled", "Adept" },
            _ => new[] { "Basic", "Simple", "Standard", "Ordinary" }
        };

        var suffixes = request.DesiredMoveType.ToLowerInvariant() switch
        {
            "ultimate" => new[] { "Oblivion", "Annihilation", "Cataclysm", "Apocalypse" },
            "special" => new[] { "Strike", "Blast", "Surge", "Burst" },
            "combo" => new[] { "Flurry", "Barrage", "Assault", "Volley" },
            "defensive" => new[] { "Guard", "Shield", "Barrier", "Ward" },
            "buff" => new[] { "Empowerment", "Enhancement", "Boost", "Surge" },
            "debuff" => new[] { "Curse", "Weakening", "Decay", "Wither" },
            _ => new[] { "Attack", "Strike", "Blow", "Hit" }
        };

        var prefix = prefixes[Random.Shared.Next(prefixes.Length)];
        var suffix = suffixes[Random.Shared.Next(suffixes.Length)];

        return $"{prefix} {suffix}";
    }
}
