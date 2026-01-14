using System;
using System.Collections.Generic;

namespace SaveState.Core.Mugen.ValueObjects;

public enum TestDifficulty
{
    Easy,
    Medium,
    Hard
}

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
        100, 0.2f, 0, 10, 5, 20, 0, 0, 0, 0, 0, 0, false, false, false, false, false, string.Empty, string.Empty, string.Empty, 0, GroundAirType.Ground, string.Empty, string.Empty);
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

public sealed class ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = Array.Empty<ValidationWarning>();
    public IReadOnlyList<string> ActionableTips { get; init; } = Array.Empty<string>();
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> MoveAnalyses { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
    public string CharacterName { get; init; } = string.Empty;
    public double BalanceScore { get; init; }
    public double PredictedWinRate { get; init; }
}

public sealed class ValidationError
{
    public string Message { get; init; } = string.Empty;
}

public sealed class ValidationWarning
{
    public string Message { get; init; } = string.Empty;
}

public sealed class TestParameters
{
    public string OpponentCharacter { get; init; } = string.Empty;
    public int TestRounds { get; init; }
    public bool UseAi { get; init; }
    public TestDifficulty Difficulty { get; init; }
    public IReadOnlyList<string> TestScenarios { get; init; } = Array.Empty<string>();

    public TestParameters()
    {
    }

    public TestParameters(string OpponentCharacter, int TestRounds, bool UseAi, TestDifficulty Difficulty, IReadOnlyList<string> TestScenarios)
    {
        this.OpponentCharacter = OpponentCharacter;
        this.TestRounds = TestRounds;
        this.UseAi = UseAi;
        this.Difficulty = Difficulty;
        this.TestScenarios = TestScenarios;
    }
}

public sealed class TestResult
{
    public bool TestPassed { get; init; }
    public IReadOnlyList<string> Findings { get; init; } = Array.Empty<string>();
}
