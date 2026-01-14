using SaveState.Core.OpenMK.ValueObjects;
using System;
using System.Collections.Generic;

namespace SaveState.Core.OpenMK.Entities;

/// <summary>
/// Represents a Mortal Kombat character used in the OpenMK integration.
/// </summary>
public sealed class OpenMKCharacter
{
    private readonly List<OpenMKSpecialMove> _specialMoves = new();
    private readonly List<OpenMKFatality> _fatalities = new();
    private readonly List<OpenMKFriendship> _friendships = new();
    private readonly List<OpenMKBrutality> _brutalities = new();
    private readonly List<OpenMKBabality> _babalityMoves = new();
    private readonly List<OpenMKCostume> _costumes = new();

    private OpenMKCharacter(
        string name,
        string displayName,
        string bio,
        OpenMKRealm realm,
        OpenMKFightingStyle fightingStyle,
        OpenMKAlignment alignment,
        string spritePath,
        string soundPath,
        string definitionPath)
    {
        Id = Guid.NewGuid();
        Name = name;
        DisplayName = displayName;
        Bio = bio;
        Realm = realm;
        FightingStyle = fightingStyle;
        Alignment = alignment;
        SpritePath = spritePath;
        SoundPath = soundPath;
        DefinitionPath = definitionPath;
        Ending = string.Empty;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public string Bio { get; }
    public OpenMKRealm Realm { get; }
    public OpenMKFightingStyle FightingStyle { get; }
    public OpenMKAlignment Alignment { get; }
    public string SpritePath { get; }
    public string SoundPath { get; }
    public string DefinitionPath { get; }
    public string Ending { get; private set; }
    public bool IsDefaultUnlocked { get; private set; } = true;
    public OpenMKUnlockRequirements UnlockRequirements { get; private set; } = new(string.Empty, OpenMKUnlockType.AlwaysUnlocked);
    public IReadOnlyList<OpenMKSpecialMove> SpecialMoves => _specialMoves;
    public IReadOnlyList<OpenMKFatality> Fatalities => _fatalities;
    public IReadOnlyList<OpenMKFriendship> Friendships => _friendships;
    public IReadOnlyList<OpenMKBrutality> Brutalities => _brutalities;
    public IReadOnlyList<OpenMKBabality> BabalityMoves => _babalityMoves;
    public IReadOnlyList<OpenMKCostume> Costumes => _costumes;

    public static OpenMKCharacter Create(
        string name,
        string displayName,
        string bio,
        OpenMKRealm realm,
        OpenMKFightingStyle fightingStyle,
        OpenMKAlignment alignment,
        string spritePath,
        string soundPath,
        string definitionPath)
    {
        return new OpenMKCharacter(name, displayName, bio, realm, fightingStyle, alignment, spritePath, soundPath, definitionPath);
    }

    public void AddSpecialMove(OpenMKSpecialMove move) => _specialMoves.Add(move);

    public void AddFatality(OpenMKFatality fatality) => _fatalities.Add(fatality);

    public void AddFriendship(OpenMKFriendship friendship) => _friendships.Add(friendship);

    public void AddBrutality(OpenMKBrutality brutality) => _brutalities.Add(brutality);

    public void AddBabality(OpenMKBabality babality) => _babalityMoves.Add(babality);

    public void AddCostume(OpenMKCostume costume) => _costumes.Add(costume);

    public void SetEnding(string ending) => Ending = ending;

    public void SetDefaultUnlocked(bool value) => IsDefaultUnlocked = value;

    public void SetUnlockRequirements(OpenMKUnlockRequirements requirements) => UnlockRequirements = requirements;
}
