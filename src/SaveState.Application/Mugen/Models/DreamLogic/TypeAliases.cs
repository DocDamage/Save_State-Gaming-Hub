// Type aliases for backward compatibility

namespace SaveState.Application.Mugen.Services;

using SaveState.Application.Mugen.Models.DreamLogic;

// Model aliases
public class DreamLogicArenaServiceDreamArena : DreamArena { }
public record DreamLogicArenaServiceArenaGeometry : ArenaGeometry { }
public class DreamLogicArenaServiceBoundary : Boundary { }
public class DreamLogicArenaServiceDreamArenaRequest : DreamArenaRequest { }
public class DreamLogicArenaServiceDreamState : DreamState { }
public class DreamLogicArenaServiceSurrealElement : SurrealElement { }
public class DreamLogicArenaServiceSymbolicElement : SymbolicElement { }
public class DreamLogicArenaServiceImpossibleGeometry : ImpossibleGeometry { }
public class DreamLogicArenaServiceGeometryTransformationRequest : GeometryTransformationRequest { }
public class DreamLogicArenaServiceSymbolicManifestation : SymbolicManifestation { }
public class DreamLogicArenaServiceSymbolicRequest : SymbolicRequest { }
public class DreamLogicArenaServiceSurrealPhysics : SurrealPhysics { }
public class DreamLogicArenaServiceSurrealEffect : SurrealEffect { }
public class DreamLogicArenaServiceSurrealEventTrigger : SurrealEventTrigger { }
public class DreamLogicArenaServiceSurrealEvent : SurrealEvent { }
public class DreamLogicArenaServiceMemoryPalace : MemoryPalace { }
public class DreamLogicArenaServiceMemoryRoom : MemoryRoom { }
public class DreamLogicArenaServiceMemoryPalaceRequest : MemoryPalaceRequest { }
public class DreamLogicArenaServiceCollectiveDream : CollectiveDream { }
public class DreamLogicArenaServiceCollectiveDreamRequest : CollectiveDreamRequest { }
public class DreamLogicArenaServiceArenaInstability : ArenaInstability { }
public class DreamLogicArenaServiceDreamAnalytics : DreamAnalytics { }
public class DreamLogicArenaServiceEmotionalImpact : EmotionalImpact { }
public class DreamLogicArenaServiceDreamEmotionalState : DreamEmotionalState { }

// Enum aliases
public enum DreamLogicArenaServiceDreamTheme { Surreal = DreamTheme.Surreal, Nightmare = DreamTheme.Nightmare, Fantasy = DreamTheme.Fantasy, Memory = DreamTheme.Memory, Collective = DreamTheme.Collective }
public enum DreamLogicArenaServiceSurfaceType { Solid = SurfaceType.Solid, Liquid = SurfaceType.Liquid, Gas = SurfaceType.Gas, Energy = SurfaceType.Energy, Void = SurfaceType.Void }
public enum DreamLogicArenaServiceBoundaryType { Wall = BoundaryType.Wall, Floor = BoundaryType.Floor, Ceiling = BoundaryType.Ceiling, Invisible = BoundaryType.Invisible }
public enum DreamLogicArenaServiceGeometryType { Euclidean = GeometryType.Euclidean, NonEuclidean = GeometryType.NonEuclidean, Warped = GeometryType.Warped, Fractal = GeometryType.Fractal }
public enum DreamLogicArenaServiceSymbolType { Heart = SymbolType.Heart, Flame = SymbolType.Flame, Water = SymbolType.Water, Light = SymbolType.Light, Shadow = SymbolType.Shadow, DreamLogicArenaServiceMemoryPalace = SymbolType.MemoryPalace }
public enum DreamLogicArenaServiceSurrealEffectType { GravityShift = SurrealEffectType.GravityShift, ObjectManifestation = SurrealEffectType.ObjectManifestation, TimeDistortion = SurrealEffectType.TimeDistortion, ObjectVanish = SurrealEffectType.ObjectVanish, RealityFracture = SurrealEffectType.RealityFracture }
public enum DreamLogicArenaServiceSurrealEventType { CombatIntensity = SurrealEventType.CombatIntensity, EmotionalPeak = SurrealEventType.EmotionalPeak, RandomManifestation = SurrealEventType.RandomManifestation, ObjectDisappearance = SurrealEventType.ObjectDisappearance, TimeAnomaly = SurrealEventType.TimeAnomaly }
public enum DreamLogicArenaServiceSurrealElementType { FloatingObject = SurrealElementType.FloatingObject, ShiftingPlatform = SurrealElementType.ShiftingPlatform, TimeAnomaly = SurrealElementType.TimeAnomaly, RealityFracture = SurrealElementType.RealityFracture }
public enum DreamLogicArenaServicePalaceLayout { Linear = PalaceLayout.Linear, Cross = PalaceLayout.Cross, Labyrinth = PalaceLayout.Labyrinth, Spiral = PalaceLayout.Spiral }
public enum DreamLogicArenaServiceRoomType { MemoryChamber = RoomType.MemoryChamber, EmotionalCore = RoomType.EmotionalCore, SymbolicHall = RoomType.SymbolicHall, DreamGate = RoomType.DreamGate }
public enum DreamLogicArenaServiceDreamRiskLevel { Low = DreamRiskLevel.Low, Medium = DreamRiskLevel.Medium, High = DreamRiskLevel.High, Critical = DreamRiskLevel.Critical }
public enum DreamLogicArenaServiceDreamEmotion { Neutral = DreamEmotion.Neutral, Joy = DreamEmotion.Joy, Anger = DreamEmotion.Anger, Fear = DreamEmotion.Fear, Confidence = DreamEmotion.Confidence, Despair = DreamEmotion.Despair, Excitement = DreamEmotion.Excitement, Calm = DreamEmotion.Calm }
