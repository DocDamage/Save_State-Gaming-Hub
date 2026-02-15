namespace SaveState.Application.Mugen.Models.NarrativeMemory;

/// <summary>
/// Memory crystal data.
/// </summary>
public record MemoryCrystal
{
    public string CrystalId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public string MatchId { get; set; } = default!;
    public MatchOutcome MatchOutcome { get; set; } = default!;
    public IReadOnlyList<string> KeyMoments { get; set; } = default!;
    public IReadOnlyList<AlternatePossibility> AlternatePossibilities { get; set; } = default!;
    public EmotionalContext EmotionalContext { get; set; } = default!;
    public CrystalRarity Rarity { get; set; } = default!;
    public decimal Value { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
    public DateTime ExpiresAt { get; set; } = default!;
}

/// <summary>
/// Alternate possibility data.
/// </summary>
public class AlternatePossibility
{
    public string Scenario { get; set; } = default!;
    public float Probability { get; set; } = default!;
    public string Outcome { get; set; } = default!;
    public int CrystalValue { get; set; } = default!;
}

/// <summary>
/// Emotional context data.
/// </summary>
public class EmotionalContext
{
    public float Intensity { get; set; } = default!;
    public string PrimaryEmotion { get; set; } = default!;
    public IReadOnlyList<string> ContributingFactors { get; set; } = default!;
}

/// <summary>
/// Crystal generation request.
/// </summary>
public class CrystalGenerationRequest
{
    public string PlayerId { get; set; } = default!;
    public bool IncludeAlternatePossibilities { get; set; } = default!;
    public CrystalRarity MinimumRarity { get; set; } = default!;
}

/// <summary>
/// Crystal collection data.
/// </summary>
public class CrystalCollection
{
    public string PlayerId { get; set; } = default!;
    public List<string> Crystals { get; set; } = default!;
    public int TotalCrystals { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
}

/// <summary>
/// Crystal enhancement request.
/// </summary>
public class CrystalEnhancementRequest
{
    public EnhancementType EnhancementType { get; set; } = default!;
    public float EnhancementStrength { get; set; } = default!;
}

/// <summary>
/// Enhancement request (alias for CrystalEnhancementRequest).
/// </summary>
public class EnhancementRequest : CrystalEnhancementRequest
{
}

/// <summary>
/// Match memory data (alias for NarrativeMatchResult).
/// </summary>
public class MatchMemory : NarrativeMatchResult
{
}

/// <summary>
/// Crystal economy data.
/// </summary>
public class CrystalEconomy
{
    public string PlayerId { get; set; } = default!;
    public int TotalCrystals { get; set; } = default!;
    public int RareCrystals { get; set; } = default!;
    public int EpicCrystals { get; set; } = default!;
    public int LegendaryCrystals { get; set; } = default!;
    public decimal CrystalValue { get; set; } = default!;
    public IReadOnlyList<TradeOpportunity> TradeOpportunities { get; set; } = default!;
    public SynthesisPotential SynthesisPotential { get; set; } = default!;
    public decimal MarketValue { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Trade opportunity data.
/// </summary>
public class TradeOpportunity
{
    public string OpportunityId { get; set; } = default!;
    public string OfferedCrystal { get; set; } = default!;
    public string RequestedCrystal { get; set; } = default!;
    public float ValueRatio { get; set; } = default!;
    public DateTime ExpiresAt { get; set; } = default!;
}

/// <summary>
/// Synthesis potential data.
/// </summary>
public class SynthesisPotential
{
    public int CompatibleCrystals { get; set; } = default!;
    public int PotentialMoves { get; set; } = default!;
    public float RarityUpgradeChance { get; set; } = default!;
    public int UniqueCombinations { get; set; } = default!;
}

/// <summary>
/// Crystal trade data.
/// </summary>
public class CrystalTrade
{
    public string TradeId { get; set; } = default!;
    public string SellerId { get; set; } = default!;
    public string BuyerId { get; set; } = default!;
    public IReadOnlyList<string> OfferedCrystals { get; set; } = default!;
    public IReadOnlyList<string> RequestedCrystals { get; set; } = default!;
    public decimal OfferedValue { get; set; } = default!;
    public decimal RequestedValue { get; set; } = default!;
    public TradeStatus Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime ExpiresAt { get; set; } = default!;
}

/// <summary>
/// Crystal trade request.
/// </summary>
public class CrystalTradeRequest
{
    public string SellerId { get; set; } = default!;
    public string BuyerId { get; set; } = default!;
    public IReadOnlyList<string> OfferedCrystals { get; set; } = default!;
    public IReadOnlyList<string> RequestedCrystals { get; set; } = default!;
}
