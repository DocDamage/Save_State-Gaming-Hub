using System.Numerics;
using SaveState.Core.Common;
using SaveState.Application.Mugen;

namespace SaveState.Application.Mugen.Services.ProceduralContentGeneration;

/// <summary>
/// Generated move data.
/// </summary>
public class ProceduralContentGeneratorGeneratedMove : ProceduralContentGeneratorGeneratedContentItem
{
    public ProceduralContentGeneratorGeneratedMove() : base("", "", DateTime.MinValue) { }
    public string MoveId { get => Id; set => Id = value; }
    public string CharacterName { get; set; } = default!;
    public ProceduralContentGeneratorMoveType MoveType { get; set; } = default!;
    public ProceduralContentGeneratorMoveParameters Parameters { get; set; } = default!;
    public ProceduralContentGeneratorProceduralAnimationData Animation { get; set; } = default!;
    public ProceduralContentGeneratorAudioData SoundEffects { get; set; } = default!;
    public double BalanceScore { get; set; } = default!;
    public double FunFactor { get; set; } = default!;
    public ProceduralContentGeneratorDifficultyLevel Difficulty { get; set; } = default!;
    public ProceduralContentGeneratorContentEvaluation Evaluation { get; set; } = default!;
    public ProceduralContentGeneratorGenerationMetadata GenerationMetadata { get; set; } = default!;
}

/// <summary>
/// Generated stage data.
/// </summary>
public class ProceduralContentGeneratorGeneratedStage : ProceduralContentGeneratorGeneratedContentItem
{
    public string StageId { get => Id; set => Id = value; }
    public string Theme { get; set; } = default!;
    public ProceduralContentGeneratorStageLayout Layout { get; set; } = default!;
    public IReadOnlyList<ProceduralContentGeneratorInteractiveElement> InteractiveElements { get; set; } = default!;
    public IReadOnlyList<ProceduralContentGeneratorEnvironmentalEffect> EnvironmentalEffects { get; set; } = default!;
    public ProceduralContentGeneratorAudioDesign AudioDesign { get; set; } = default!;
    public StageDimensions Dimensions { get; set; } = default!;
    public double BalanceScore { get; set; } = default!;
    public double VisualAppeal { get; set; } = default!;
    public double GameplayImpact { get; set; } = default!;
    public ProceduralContentGeneratorGenerationMetadata GenerationMetadata { get; set; } = default!;
}

/// <summary>
/// Generated character data.
/// </summary>
public class ProceduralContentGeneratorGeneratedCharacter : ProceduralContentGeneratorGeneratedContentItem
{
    public string CharacterId { get => Id; set => Id = value; }
    public ProceduralContentGeneratorCharacterArchetype Archetype { get; set; } = default!;
    public ProceduralContentGeneratorCharacterAttributes Attributes { get; set; } = default!;
    public ProceduralContentGeneratorCharacterMoveset Moveset { get; set; } = default!;
    public ProceduralContentGeneratorVisualDesign VisualDesign { get; set; } = default!;
    public ProceduralContentGeneratorAIData AIBehavior { get; set; } = default!;
    public string Lore { get; set; } = default!;
    public double BalanceScore { get; set; } = default!;
    public double UniquenessScore { get; set; } = default!;
    public double SkillCeiling { get; set; } = default!;
    public ProceduralContentGeneratorGenerationMetadata GenerationMetadata { get; set; } = default!;
}

/// <summary>
/// Generated effect data.
/// </summary>
public class ProceduralContentGeneratorGeneratedEffect : ProceduralContentGeneratorGeneratedContentItem
{
    public string EffectId { get => Id; set => Id = value; }
    public ProceduralContentGeneratorEffectType EffectType { get; set; } = default!;
    public ProceduralContentGeneratorEffectParameters Parameters { get; set; } = default!;
    public ProceduralContentGeneratorVisualRepresentation VisualRepresentation { get; set; } = default!;
    public ProceduralContentGeneratorAudioComponent AudioComponent { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public IReadOnlyList<string> TriggerConditions { get; set; } = default!;
    public double ImpactScore { get; set; } = default!;
    public double VisualQuality { get; set; } = default!;
    public double AudioQuality { get; set; } = default!;
    public ProceduralContentGeneratorGenerationMetadata GenerationMetadata { get; set; } = default!;
}

/// <summary>
/// Content collection data.
/// </summary>
public class ProceduralContentGeneratorContentCollection
{
    public string CollectionId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Theme { get; set; } = default!;
    public IReadOnlyList<ProceduralContentGeneratorGeneratedContentItem> Items { get; set; } = default!;
    public double CoherenceScore { get; set; } = default!;
    public double QualityScore { get; set; } = default!;
    public double CompletenessScore { get; set; } = default!;
    public ProceduralContentGeneratorGenerationMetadata GenerationMetadata { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Content evolution data.
/// </summary>
public class ProceduralContentGeneratorContentEvolution
{
    public string EvolutionId { get; set; } = default!;
    public string OriginalContentId { get; set; } = default!;
    public ProceduralContentGeneratorGeneratedContentItem EvolvedContent { get; set; } = default!;
    public ProceduralContentGeneratorEvolutionStrategy EvolutionStrategy { get; set; } = default!;
    public IReadOnlyList<string> ChangesApplied { get; set; } = default!;
    public double QualityImprovement { get; set; } = default!;
    public double BalanceChange { get; set; } = default!;
    public double UniquenessIncrease { get; set; } = default!;
    public ProceduralContentGeneratorGenerationMetadata GenerationMetadata { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Move generation request.
/// </summary>
public class ProceduralContentGeneratorMoveGenerationRequest
{
    public string Name { get; set; } = default!;
    public string CharacterName { get; set; } = default!;
    public ProceduralContentGeneratorMoveType MoveType { get; set; } = default!;
    public double PowerLevel { get; set; } = default!;
    public IReadOnlyList<string> RequiredMechanics { get; set; } = default!;
    public ProceduralContentGeneratorDifficultyLevel Difficulty { get; set; } = default!;
}

/// <summary>
/// Stage generation request.
/// </summary>
public class ProceduralContentGeneratorStageGenerationRequest
{
    public string Name { get; set; } = default!;
    public string Theme { get; set; } = default!;
    public StageDimensions Dimensions { get; set; } = default!;
    public ProceduralContentGeneratorDifficultyLevel Difficulty { get; set; } = default!;
    public bool IncludeDestructible { get; set; } = default!;
    public bool IncludeMovingPlatforms { get; set; } = default!;
    public ProceduralContentGeneratorWeatherType WeatherType { get; set; } = default!;
}

/// <summary>
/// Character generation request.
/// </summary>
public class ProceduralContentGeneratorCharacterGenerationRequest
{
    public string Name { get; set; } = default!;
    public ProceduralContentGeneratorCharacterArchetype Archetype { get; set; } = default!;
    public ProceduralContentGeneratorDifficultyLevel Difficulty { get; set; } = default!;
    public string? StyleInfluence { get; set; } = default!;
}

/// <summary>
/// Effect generation request.
/// </summary>
public class ProceduralContentGeneratorEffectGenerationRequest
{
    public string Name { get; set; } = default!;
    public ProceduralContentGeneratorEffectType EffectType { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public IReadOnlyList<string> TriggerConditions { get; set; } = default!;
}

/// <summary>
/// Collection generation request.
/// </summary>
public class ProceduralContentGeneratorCollectionGenerationRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Theme { get; set; } = default!;
    public int ItemCount { get; set; } = default!;
    public IReadOnlyList<ProceduralContentGeneratorContentType> ContentTypes { get; set; } = default!;
}

/// <summary>
/// Evolution request.
/// </summary>
public class ProceduralContentGeneratorEvolutionRequest
{
    public ProceduralContentGeneratorEvolutionStrategy EvolutionStrategy { get; set; } = default!;
    public IReadOnlyList<string> TargetImprovements { get; set; } = default!;
    public double EvolutionStrength { get; set; } = default!;
}

/// <summary>
/// Move parameters data.
/// </summary>
public record ProceduralContentGeneratorMoveParameters
{
    public int Damage { get; set; } = default!;
    public int StartupFrames { get; set; } = default!;
    public int ActiveFrames { get; set; } = default!;
    public int RecoveryFrames { get; set; } = default!;
    public int Range { get; set; } = default!;
    public int Hitstun { get; set; } = default!;
    public int Blockstun { get; set; } = default!;
    public int Knockback { get; set; } = default!;
    public int MeterGain { get; set; } = default!;
    public bool IsProjectile { get; set; } = default!;
    public bool IsAntiAir { get; set; } = default!;
    public bool IsThrow { get; set; } = default!;
}

/// <summary>
/// Animation data.
/// </summary>
public class ProceduralContentGeneratorProceduralAnimationData
{
    public int FrameCount { get; set; } = default!;
    public IReadOnlyList<int> KeyFrames { get; set; } = default!;
    public IReadOnlyList<string> Sprites { get; set; } = default!;
    public IReadOnlyList<int> Timing { get; set; } = default!;
}

/// <summary>
/// Audio data.
/// </summary>
public class ProceduralContentGeneratorAudioData
{
    public IReadOnlyList<string> SoundFiles { get; set; } = default!;
    public IReadOnlyList<float> VolumeLevels { get; set; } = default!;
    public IReadOnlyList<float> PitchVariations { get; set; } = default!;
}

/// <summary>
/// Stage layout data.
/// </summary>
public class ProceduralContentGeneratorStageLayout
{
    public int Width { get; set; } = default!;
    public int Height { get; set; } = default!;
    public IReadOnlyList<ProceduralContentGeneratorPlatform> Platforms { get; set; } = default!;
    public IReadOnlyList<ProceduralContentGeneratorBackgroundLayer> BackgroundLayers { get; set; } = default!;
    public ProceduralContentGeneratorRectangle CameraBounds { get; set; } = default!;
    public IReadOnlyList<Vector2> SpawnPoints { get; set; } = default!;
}

/// <summary>
/// Platform data.
/// </summary>
public class ProceduralContentGeneratorPlatform
{
    public ProceduralContentGeneratorProceduralVector2 Position { get; set; } = default!;
    public int Width { get; set; } = default!;
    public int Height { get; set; } = default!;
    public string Type { get; set; } = default!;
}

/// <summary>
/// Background layer data.
/// </summary>
public class ProceduralContentGeneratorBackgroundLayer
{
    public string Image { get; set; } = default!;
    public float ParallaxFactor { get; set; } = default!;
    public int Depth { get; set; } = default!;
}

/// <summary>
/// Interactive element data.
/// </summary>
public class ProceduralContentGeneratorInteractiveElement
{
    public string ElementId { get; set; } = default!;
    public ProceduralContentGeneratorElementType Type { get; set; } = default!;
    public ProceduralContentGeneratorProceduralVector2 Position { get; set; } = default!;
    public int? Health { get; set; } = default!;
    public TimeSpan? RespawnTime { get; set; } = default!;
    public IReadOnlyList<Vector2>? MovementPath { get; set; } = default!;
    public float? MovementSpeed { get; set; } = default!;
}

/// <summary>
/// Environmental effect data.
/// </summary>
public class ProceduralContentGeneratorEnvironmentalEffect
{
    public string EffectId { get; set; } = default!;
    public ProceduralContentGeneratorEffectType Type { get; set; } = default!;
    public ProceduralContentGeneratorWeatherType? WeatherType { get; set; } = default!;
    public string? ParticleType { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
}

/// <summary>
/// Audio design data.
/// </summary>
public class ProceduralContentGeneratorAudioDesign
{
    public string BackgroundMusic { get; set; } = default!;
    public IReadOnlyList<string> AmbientSounds { get; set; } = default!;
    public IReadOnlyList<string> InteractiveSounds { get; set; } = default!;
    public ProceduralContentGeneratorProceduralReverbSettings ReverbSettings { get; set; } = default!;
}

/// <summary>
/// Character attributes data.
/// </summary>
public class ProceduralContentGeneratorCharacterAttributes
{
    public int Health { get; set; } = default!;
    public int Attack { get; set; } = default!;
    public int Defense { get; set; } = default!;
    public int Speed { get; set; } = default!;
    public string SpecialAbility { get; set; } = default!;
}

/// <summary>
/// Character moveset data.
/// </summary>
public class ProceduralContentGeneratorCharacterMoveset
{
    public IReadOnlyList<ProceduralContentGeneratorCharacterMove> Moves { get; set; } = default!;
    public IReadOnlyList<ProceduralContentGeneratorComboData> Combos { get; set; } = default!;
    public ProceduralContentGeneratorMeterSystem MeterSystem { get; set; } = default!;
}

/// <summary>
/// Character move data.
/// </summary>
public class ProceduralContentGeneratorCharacterMove
{
    public string Name { get; set; } = default!;
    public ProceduralContentGeneratorMoveType Type { get; set; } = default!;
    public int Damage { get; set; } = default!;
    public int Range { get; set; } = default!;
    public bool Projectile { get; set; } = default!;
    public bool AntiAir { get; set; } = default!;
}

/// <summary>
/// Combo data.
/// </summary>
public class ProceduralContentGeneratorComboData
{
    public string Name { get; set; } = default!;
    public IReadOnlyList<string> Moves { get; set; } = default!;
    public int Damage { get; set; } = default!;
    public string Difficulty { get; set; } = default!;
}

/// <summary>
/// Meter system data.
/// </summary>
public class ProceduralContentGeneratorMeterSystem
{
    public int MaxMeter { get; set; } = default!;
    public int SuperCost { get; set; } = default!;
    public int RegenRate { get; set; } = default!;
}

/// <summary>
/// Visual design data.
/// </summary>
public class ProceduralContentGeneratorVisualDesign
{
    public string SpriteSheet { get; set; } = default!;
    public IReadOnlyList<string> ColorPalette { get; set; } = default!;
    public string AnimationStyle { get; set; } = default!;
    public IReadOnlyList<string> Effects { get; set; } = default!;
}

/// <summary>
/// AI data.
/// </summary>
public class ProceduralContentGeneratorAIData
{
    public string Difficulty { get; set; } = default!;
    public IReadOnlyList<string> BehaviorPatterns { get; set; } = default!;
    public IReadOnlyDictionary<string, double> DecisionWeights { get; set; } = default!;
}

/// <summary>
/// Effect parameters data.
/// </summary>
public class ProceduralContentGeneratorEffectParameters
{
    public float Scale { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public string Color { get; set; } = default!;
}

/// <summary>
/// Visual representation data.
/// </summary>
public class ProceduralContentGeneratorVisualRepresentation
{
    public IReadOnlyList<string> SpriteSequence { get; set; } = default!;
    public string ParticleSystem { get; set; } = default!;
    public string ShaderEffect { get; set; } = default!;
}

/// <summary>
/// Audio component data.
/// </summary>
public class ProceduralContentGeneratorAudioComponent
{
    public string SoundFile { get; set; } = default!;
    public float Volume { get; set; } = default!;
    public float Pitch { get; set; } = default!;
    public bool Looping { get; set; } = default!;
}

/// <summary>
/// Content evaluation data.
/// </summary>
public class ProceduralContentGeneratorContentEvaluation
{
    public double BalanceScore { get; set; } = default!;
    public double FunFactor { get; set; } = default!;
    public ProceduralContentGeneratorDifficultyLevel Difficulty { get; set; } = default!;
    public IReadOnlyList<string> Recommendations { get; set; } = default!;
    public double VisualAppeal { get; set; } = default!;
    public double GameplayImpact { get; set; } = default!;
    public double UniquenessScore { get; set; } = default!;
    public double SkillCeiling { get; set; } = default!;
    public double ImpactScore { get; set; } = default!;
    public double VisualQuality { get; set; } = default!;
    public double AudioQuality { get; set; } = default!;
}

/// <summary>
/// Collection evaluation data.
/// </summary>
public class ProceduralContentGeneratorCollectionEvaluation
{
    public double CoherenceScore { get; set; } = default!;
    public double QualityScore { get; set; } = default!;
    public double CompletenessScore { get; set; } = default!;
}

/// <summary>
/// Evolution evaluation data.
/// </summary>
public class ProceduralContentGeneratorEvolutionEvaluation
{
    public double QualityImprovement { get; set; } = default!;
    public double BalanceChange { get; set; } = default!;
    public double UniquenessIncrease { get; set; } = default!;
}

/// <summary>
/// Character style analysis data.
/// </summary>
public class ProceduralContentGeneratorCharacterStyleAnalysis
{
    public string PrimaryStyle { get; set; } = default!;
    public IReadOnlyList<string> SecondaryStyles { get; set; } = default!;
    public IReadOnlyList<string> MovePreferences { get; set; } = default!;
    public IReadOnlyList<string> SimilarMoves { get; set; } = default!;
    public double StyleConsistency { get; set; } = default!;
}

/// <summary>
/// Generation metadata.
/// </summary>
public class ProceduralContentGeneratorGenerationMetadata
{
    public string GeneratorVersion { get; set; } = default!;
    public DateTime GenerationTime { get; set; } = default!;
    public string AlgorithmUsed { get; set; } = default!;
    public string StyleInfluence { get; set; } = default!;
    public IReadOnlyList<string>? InspirationMoves { get; set; } = default!;
    public IReadOnlyList<string>? InspirationCharacters { get; set; } = default!;
    public IReadOnlyList<string>? InspirationStages { get; set; } = default!;
    public IReadOnlyList<string>? InspirationContent { get; set; } = default!;
}

/// <summary>
/// Base class for generated content items.
/// </summary>
public abstract class ProceduralContentGeneratorGeneratedContentItem
{
    public ProceduralContentGeneratorGeneratedContentItem() { }
    public ProceduralContentGeneratorGeneratedContentItem(string id, string name, DateTime generatedAt)
    {
        Id = id;
        Name = name;
        GeneratedAt = generatedAt;
    }

    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Various enumeration types.
/// </summary>
public enum ProceduralContentGeneratorMoveType { Normal, Special, Super }
public enum ProceduralContentGeneratorCharacterArchetype { Rushdown, Zoning, Grappler, AllRounder, Custom }
public enum ProceduralContentGeneratorEffectType { Explosion, Magic, Impact, Weather, Particles }
public enum ProceduralContentGeneratorElementType { Destructible, MovingPlatform, Interactive, Environmental }
public enum ProceduralContentGeneratorContentType { Move, Stage, Character, Effect }
public enum ProceduralContentGeneratorEvolutionStrategy { Genetic, MLGuided, PlayerFeedback, Random }
public enum ProceduralContentGeneratorWeatherType { Clear, Rain, Snow, Fog, Wind }
public enum ProceduralContentGeneratorDifficultyLevel { Easy, Medium, Hard, VeryHard }

/// <summary>
/// Vector2 and other utility records.
/// </summary>
public class ProceduralContentGeneratorProceduralVector2
{
    public ProceduralContentGeneratorProceduralVector2() { }
    public ProceduralContentGeneratorProceduralVector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; set; } = default!;
    public float Y { get; set; } = default!;
}

public class ProceduralContentGeneratorRectangle
{
    public ProceduralContentGeneratorRectangle() { }
    public ProceduralContentGeneratorRectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; set; } = default!;
    public int Y { get; set; } = default!;
    public int Width { get; set; } = default!;
    public int Height { get; set; } = default!;
}

public class ProceduralContentGeneratorProceduralReverbSettings
{
    public float RoomSize { get; set; } = default!;
    public float Damping { get; set; } = default!;
    public float WetLevel { get; set; } = default!;
    public float DryLevel { get; set; } = default!;
    public float PreDelay { get; set; } = default!;
}

/// <summary>
/// Procedural Content Generator interface.
/// </summary>
public interface IProceduralContentGenerator
{
    Task<Result<ProceduralContentGeneratorGeneratedMove>> GenerateMoveAsync(ProceduralContentGeneratorMoveGenerationRequest request, CancellationToken ct = default);
    Task<Result<ProceduralContentGeneratorGeneratedStage>> GenerateStageAsync(ProceduralContentGeneratorStageGenerationRequest request, CancellationToken ct = default);
    Task<Result<ProceduralContentGeneratorGeneratedCharacter>> GenerateCharacterAsync(ProceduralContentGeneratorCharacterGenerationRequest request, CancellationToken ct = default);
    Task<Result<ProceduralContentGeneratorGeneratedEffect>> GenerateEffectAsync(ProceduralContentGeneratorEffectGenerationRequest request, CancellationToken ct = default);
    Task<Result<ProceduralContentGeneratorContentCollection>> GenerateContentCollectionAsync(ProceduralContentGeneratorCollectionGenerationRequest request, CancellationToken ct = default);
    Task<Result<ProceduralContentGeneratorContentEvolution>> EvolveContentAsync(string contentId, ProceduralContentGeneratorEvolutionRequest request, CancellationToken ct = default);
}
