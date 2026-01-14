using System;
using System.Collections.Generic;

namespace SaveState.Core.Mugen.ValueObjects;

public sealed class MoveTemplate
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public MoveCategory Category { get; init; }
    public DifficultyLevel Difficulty { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public string Description { get; init; } = string.Empty;
    public MoveType Type { get; init; }
    public MoveType MoveType => Type;
}

public sealed class MoveDefinition
{
    public string DisplayName { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public MoveType MoveType { get; init; }
    public MoveCategory Category { get; init; }
    public MoveProperties Properties { get; init; } = new MoveProperties(
        Damage: 100,
        MeterGain: 0,
        MeterCost: 0,
        StartupFrames: 10,
        ActiveFrames: 5,
        RecoveryFrames: 20,
        FrameAdvantageOnHit: 0,
        FrameAdvantageOnBlock: 0,
        HitStun: 0,
        BlockStun: 0,
        HitStop: 0,
        BlockStop: 0,
        CausesKnockdown: false,
        GuardCrush: false,
        CounterHit: false,
        Unblockable: false,
        ArmorBreak: false,
        KnockdownType: KnockdownType.None,
        HitEffect: HitEffect.Light,
        GuardEffect: GuardEffect.Light,
        Priority: Priority.Medium,
        GroundAirType: GroundAirType.Ground,
        Attribute: MoveAttribute.Normal,
        Flags: Array.Empty<string>());
}

public sealed class ValidationOptions
{
    public bool CheckFrameData { get; init; }
    public bool CheckHitboxes { get; init; }
    public bool CheckBalance { get; init; }
    public bool CheckCommands { get; init; }
    public bool StrictMode { get; init; }
    public IReadOnlyList<string> CustomRules { get; init; } = Array.Empty<string>();

    public ValidationOptions()
    {
    }

    public ValidationOptions(
        bool CheckFrameData,
        bool CheckHitboxes,
        bool CheckBalance,
        bool CheckCommands,
        bool StrictMode,
        IReadOnlyList<string> CustomRules)
    {
        this.CheckFrameData = CheckFrameData;
        this.CheckHitboxes = CheckHitboxes;
        this.CheckBalance = CheckBalance;
        this.CheckCommands = CheckCommands;
        this.StrictMode = StrictMode;
        this.CustomRules = CustomRules;
    }
}

public sealed class TestResult
{
    public bool TestPassed { get; init; }
    public IReadOnlyList<string> Findings { get; init; } = Array.Empty<string>();
}
