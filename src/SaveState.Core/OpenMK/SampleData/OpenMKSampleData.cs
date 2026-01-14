using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.ValueObjects;

namespace SaveState.Core.OpenMK.SampleData;

/// <summary>
/// Sample data for OpenMK characters to demonstrate the integration.
/// </summary>
public static class OpenMKSampleData
{
    /// <summary>
    /// Creates a sample Liu Kang character.
    /// </summary>
    public static OpenMKCharacter CreateLiuKang()
    {
        var character = OpenMKCharacter.Create(
            name: "liu_kang",
            displayName: "Liu Kang",
            bio: "A Shaolin monk and champion of Earthrealm, Liu Kang is known for his disciplined fighting style and dedication to justice. Once a friend to Kung Lao and Johnny Cage, he has become a legendary warrior in the battle against Outworld.",
            realm: OpenMKRealm.Earthrealm,
            fightingStyle: OpenMKFightingStyle.KungFu,
            alignment: OpenMKAlignment.Good,
            spritePath: "chars/liu_kang/",
            soundPath: "chars/liu_kang/sound/",
            definitionPath: "chars/liu_kang/liu_kang.def"
        );

        // Add special moves
        character.AddSpecialMove(new OpenMKSpecialMove(
            Name: "fireball",
            DisplayName: "Fireball",
            Description: "Launches a fireball projectile",
            InputCommand: "Forward, Forward, Punch",
            Type: OpenMKSpecialMoveType.Special,
            Damage: 80,
            SoundEffect: "fireball.wav"
        ));

        character.AddSpecialMove(new OpenMKSpecialMove(
            Name: "flying_dragon_kick",
            DisplayName: "Flying Dragon Kick",
            Description: "A powerful flying kick attack",
            InputCommand: "Forward, Forward, Kick",
            Type: OpenMKSpecialMoveType.Special,
            Damage: 100,
            SoundEffect: "dragon_kick.wav"
        ));

        character.AddSpecialMove(new OpenMKSpecialMove(
            Name: "bicycle_kick",
            DisplayName: "Bicycle Kick",
            Description: "Rapid spinning kick combo",
            InputCommand: "Back, Forward, Kick",
            Type: OpenMKSpecialMoveType.Enhanced,
            Damage: 120,
            SoundEffect: "bicycle.wav"
        ));

        // Add fatalities
        character.AddFatality(new OpenMKFatality(
            Name: "dragon_fatality",
            DisplayName: "Dragon Fatality",
            Description: "Summons a dragon to consume the opponent",
            InputCommand: "Forward, Back, Forward, Back, Punch",
            Type: OpenMKFatalityType.Standard,
            AnimationSequence: "fatality_dragon",
            SoundEffect: "dragon_roar.wav",
            VoiceLine: "You will never win!"
        ));

        character.AddFatality(new OpenMKFatality(
            Name: "cartwheel_uppercut",
            DisplayName: "Cartwheel Uppercut",
            Description: "Cartwheel into an uppercut that decapitates",
            InputCommand: "Up, Up, Forward, Punch",
            Type: OpenMKFatalityType.Standard,
            AnimationSequence: "fatality_cartwheel",
            SoundEffect: "uppercut.wav",
            VoiceLine: "Feel the power of the Shaolin!"
        ));

        // Add friendship
        character.AddFriendship(new OpenMKFriendship(
            Name: "shaolin_soccer",
            DisplayName: "Shaolin Soccer",
            Description: "Plays soccer with opponent's head",
            InputCommand: "Back, Back, Down, Kick",
            AnimationSequence: "friendship_soccer",
            SoundEffect: "soccer_kick.wav",
            VoiceLine: "Time for some fun!",
            ItemUsed: "Soccer Ball"
        ));

        // Add brutality
        character.AddBrutality(new OpenMKBrutality(
            Name: "bone_breaker",
            DisplayName: "Bone Breaker",
            Description: "Breaks opponent's bones with precision strikes",
            InputCommand: "Forward, Down, Back, Punch",
            Requirements: new[] { "Break 3 limbs", "No throws used" },
            AnimationSequence: "brutality_bones",
            SoundEffect: "bone_crack.wav",
            VoiceLine: "Your bones are mine!"
        ));

        // Add babality
        character.AddBabality(new OpenMKBabality(
            Name: "baby_kang",
            DisplayName: "Baby Liu Kang",
            Description: "Turns opponent into a baby version of Liu Kang",
            InputCommand: "Down, Back, Down, Forward, Punch",
            AnimationSequence: "babality_kang",
            SoundEffect: "baby_cry.wav",
            VoiceLine: "What a cute baby!",
            BabyItem: "Pacifier"
        ));

        // Add costumes
        character.AddCostume(new OpenMKCostume(
            name: "classic",
            displayName: "Classic",
            description: "Traditional Shaolin monk attire",
            spritePath: "chars/liu_kang/classic/",
            isDefault: true
        ));

        character.AddCostume(new OpenMKCostume(
            name: "dragon",
            displayName: "Dragon",
            description: "Decorated with dragon motifs",
            spritePath: "chars/liu_kang/dragon/",
            isDefault: false,
            unlockRequirements: new OpenMKUnlockRequirements(
                description: "Complete Story Mode",
                type: OpenMKUnlockType.StoryMode
            )
        ));

        // Set ending
        character.SetEnding(
            "Liu Kang returned to the Shaolin Temple as a hero. His victory over Shao Kahn ensured peace between the realms. " +
            "He continued to train new warriors and protect Earthrealm from future threats, becoming a living legend among mortals and gods alike."
        );

        return character;
    }

    /// <summary>
    /// Creates a sample Kung Lao character.
    /// </summary>
    public static OpenMKCharacter CreateKungLao()
    {
        var character = OpenMKCharacter.Create(
            name: "kung_lao",
            displayName: "Kung Lao",
            bio: "A descendant of the Great Kung Lao, this Shaolin monk wields a razor-brimmed hat as his signature weapon. Known for his speed and acrobatic fighting style.",
            realm: OpenMKRealm.Earthrealm,
            fightingStyle: OpenMKFightingStyle.WuShu,
            alignment: OpenMKAlignment.Good,
            spritePath: "chars/kung_lao/",
            soundPath: "chars/kung_lao/sound/",
            definitionPath: "chars/kung_lao/kung_lao.def"
        );

        // Add special moves
        character.AddSpecialMove(new OpenMKSpecialMove(
            Name: "hat_throw",
            DisplayName: "Hat Throw",
            Description: "Throws razor-brimmed hat as a projectile",
            InputCommand: "Back, Forward, Punch",
            Type: OpenMKSpecialMoveType.Weapon,
            Damage: 90,
            SoundEffect: "hat_throw.wav"
        ));

        character.AddSpecialMove(new OpenMKSpecialMove(
            Name: "dive_kick",
            DisplayName: "Dive Kick",
            Description: "Flying dive kick from above",
            InputCommand: "Down, Forward, Kick",
            Type: OpenMKSpecialMoveType.Special,
            Damage: 85,
            SoundEffect: "dive.wav"
        ));

        // Add fatality
        character.AddFatality(new OpenMKFatality(
            Name: "hat_decapitation",
            DisplayName: "Hat Decapitation",
            Description: "Throws hat to decapitate opponent",
            InputCommand: "Up, Up, Forward, Back, Punch",
            Type: OpenMKFatalityType.Standard,
            AnimationSequence: "fatality_hat",
            SoundEffect: "hat_slice.wav",
            VoiceLine: "Your soul is mine!"
        ));

        character.SetEnding(
            "Kung Lao fulfilled his ancestor's legacy by defeating Shao Kahn. He became the new guardian of the Shaolin Temple, " +
            "training future generations and ensuring the safety of Earthrealm through wisdom and combat prowess."
        );

        return character;
    }

    /// <summary>
    /// Creates a sample Johnny Cage character.
    /// </summary>
    public static OpenMKCharacter CreateJohnnyCage()
    {
        var character = OpenMKCharacter.Create(
            name: "johnny_cage",
            displayName: "Johnny Cage",
            bio: "Hollywood action star turned warrior. Johnny Cage brings his martial arts skills and ego to the Mortal Kombat tournament.",
            realm: OpenMKRealm.Earthrealm,
            fightingStyle: OpenMKFightingStyle.MMA,
            alignment: OpenMKAlignment.Neutral,
            spritePath: "chars/johnny_cage/",
            soundPath: "chars/johnny_cage/sound/",
            definitionPath: "chars/johnny_cage/johnny_cage.def"
        );

        // Add special moves
        character.AddSpecialMove(new OpenMKSpecialMove(
            Name: "shadow_kick",
            DisplayName: "Shadow Kick",
            Description: "Quick shadow kick projectile",
            InputCommand: "Back, Forward, Kick",
            Type: OpenMKSpecialMoveType.Special,
            Damage: 75,
            SoundEffect: "shadow.wav"
        ));

        character.AddSpecialMove(new OpenMKSpecialMove(
            Name: "nut_punch",
            DisplayName: "Nut Punch",
            Description: "Low attack to opponent's groin",
            InputCommand: "Back, Forward, Punch",
            Type: OpenMKSpecialMoveType.Special,
            Damage: 95,
            SoundEffect: "nut_punch.wav"
        ));

        // Add fatality
        character.AddFatality(new OpenMKFatality(
            Name: "head_pop",
            DisplayName: "Head Pop",
            Description: "Pops opponent's head like a zit",
            InputCommand: "Forward, Forward, Back, Back, Punch",
            Type: OpenMKFatalityType.Standard,
            AnimationSequence: "fatality_headpop",
            SoundEffect: "head_pop.wav",
            VoiceLine: "You're gonna feel that in the morning!"
        ));

        character.SetEnding(
            "Johnny Cage returned to Hollywood as a bigger star than ever. His experiences in the tournament became the basis for his most acclaimed films. " +
            "Though he continued fighting supernatural threats, he never lost his Hollywood charm and ego."
        );

        return character;
    }

    /// <summary>
    /// Gets all sample OpenMK characters.
    /// </summary>
    public static IEnumerable<OpenMKCharacter> GetAllSampleCharacters()
    {
        yield return CreateLiuKang();
        yield return CreateKungLao();
        yield return CreateJohnnyCage();
    }
}
