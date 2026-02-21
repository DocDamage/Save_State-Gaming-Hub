using System.Text.RegularExpressions;

namespace SaveState.Core.Mugen.CharacterFrameAnalysis;

/// <summary>
/// Analyzes MUGEN character files to extract frame data.
/// </summary>
public class FrameDataAnalyzer
{
    /// <summary>
    /// Parses a character's .air file to extract frame data.
    /// </summary>
    public CharacterFrameData ParseCharacterFrameData(string characterPath)
    {
        var characterName = Path.GetFileName(characterPath);
        var frameData = new CharacterFrameData
        {
            CharacterName = characterName,
            LastUpdated = DateTime.UtcNow
        };

        var airFilePath = Path.Combine(characterPath, $"{characterName}.air");
        if (File.Exists(airFilePath))
        {
            ParseAirFile(airFilePath, frameData);
        }

        var cmdFilePath = Path.Combine(characterPath, $"{characterName}.cmd");
        if (File.Exists(cmdFilePath))
        {
            ParseCmdFile(cmdFilePath, frameData);
        }

        return frameData;
    }

    private void ParseAirFile(string airFilePath, CharacterFrameData frameData)
    {
        var lines = File.ReadAllLines(airFilePath);
        var currentAction = -1;
        var currentClsn = new List<ClsnBox>();
        var currentFrameCount = 0;

        foreach (var line in lines)
        {
            // Parse action begin
            if (line.StartsWith("[Begin Action "))
            {
                if (currentAction != -1 && currentFrameCount > 0)
                {
                    ProcessAction(currentAction, currentFrameCount, currentClsn, frameData);
                }
                
                currentAction = ParseActionNumber(line);
                currentClsn.Clear();
                currentFrameCount = 0;
            }
            
            // Parse CLSN (collision boxes)
            if (line.StartsWith("Clsn"))
            {
                var clsn = ParseClsnLine(line);
                if (clsn != null) currentClsn.Add(clsn);
            }
            
            // Parse frame
            if (line.Trim().StartsWith(""))
            {
                var frame = ParseFrameLine(line);
                if (frame.HasValue)
                {
                    currentFrameCount += frame.Value.tickCount;
                }
            }
        }

        // Process last action
        if (currentAction != -1 && currentFrameCount > 0)
        {
            ProcessAction(currentAction, currentFrameCount, currentClsn, frameData);
        }
    }

    private void ParseCmdFile(string cmdFilePath, CharacterFrameData frameData)
    {
        var lines = File.ReadAllLines(cmdFilePath);
        
        foreach (var line in lines)
        {
            // Parse command definitions
            if (line.StartsWith("[Command]"))
            {
                // Would parse command triggers
            }
            
            // Parse state definitions
            if (line.StartsWith("[Statedef "))
            {
                // Would parse state properties
            }
        }
    }

    private void ProcessAction(int actionNumber, int frameCount, List<ClsnBox> clsns, CharacterFrameData frameData)
    {
        // MUGEN action numbers:
        // 0 = standing idle
        // 5-9 = standing light/medium/heavy punches/kicks
        // 10-19 = crouching normals
        // 600-699 = standing LP/LK/MP/MK/HP/HK
        // etc.
        
        var move = new MoveFrameData();
        
        switch (actionNumber)
        {
            case 200:
                move.MoveName = "Standing Light Punch";
                move.Command = "LP";
                frameData.StandingNormals.Add(move);
                break;
            case 210:
                move.MoveName = "Standing Medium Punch";
                move.Command = "MP";
                frameData.StandingNormals.Add(move);
                break;
            case 220:
                move.MoveName = "Standing Heavy Punch";
                move.Command = "HP";
                frameData.StandingNormals.Add(move);
                break;
            case 230:
                move.MoveName = "Standing Light Kick";
                move.Command = "LK";
                frameData.StandingNormals.Add(move);
                break;
            case 240:
                move.MoveName = "Standing Medium Kick";
                move.Command = "MK";
                frameData.StandingNormals.Add(move);
                break;
            case 250:
                move.MoveName = "Standing Heavy Kick";
                move.Command = "HK";
                frameData.StandingNormals.Add(move);
                break;
        }
        
        // TotalFrames is calculated property - frameCount is distributed among startup/active/recovery
        
        // Detect hitboxes to determine active frames
        var hitboxes = clsns.Where(c => c.Type == ClsnType.Hitbox).ToList();
        if (hitboxes.Any())
        {
            move.ActiveFrames = hitboxes.Count * 2; // Rough estimate
            move.StartupFrames = Math.Max(1, frameCount - move.ActiveFrames - 10);
            move.RecoveryFrames = frameCount - move.StartupFrames - move.ActiveFrames;
        }
        else
        {
            move.StartupFrames = frameCount;
            move.ActiveFrames = 0;
            move.RecoveryFrames = 0;
        }
    }

    private int ParseActionNumber(string line)
    {
        var match = Regex.Match(line, @"\[Begin Action (\d+)\]");
        return match.Success ? int.Parse(match.Groups[1].Value) : -1;
    }

    private ClsnBox? ParseClsnLine(string line)
    {
        // Format: Clsn1[0] = -10, -90, 30, 0
        var match = Regex.Match(line, @"Clsn(\d)\[(\d+)\] = ([-\d]+), ([-\d]+), ([-\d]+), ([-\d]+)");
        if (!match.Success) return null;

        return new ClsnBox
        {
            Type = match.Groups[1].Value == "1" ? ClsnType.Hitbox : ClsnType.Hurtbox,
            Index = int.Parse(match.Groups[2].Value),
            X1 = int.Parse(match.Groups[3].Value),
            Y1 = int.Parse(match.Groups[4].Value),
            X2 = int.Parse(match.Groups[5].Value),
            Y2 = int.Parse(match.Groups[6].Value)
        };
    }

    private (int element, int index, int tickCount)? ParseFrameLine(string line)
    {
        // Format: Element, Index, X, Y, TickCount
        var parts = line.Split(',').Select(p => p.Trim()).ToArray();
        if (parts.Length >= 5 && 
            int.TryParse(parts[1], out int index) &&
            int.TryParse(parts[4], out int tickCount))
        {
            return (0, index, tickCount);
        }
        return null;
    }

    /// <summary>
    /// Analyzes frame data to identify punishable moves.
    /// </summary>
    public List<PunishableMove> FindPunishableMoves(CharacterFrameData frameData, int moveSpeed = 5)
    {
        var punishable = new List<PunishableMove>();
        
        foreach (var move in frameData.AllMoves)
        {
            if (move.BlockAdvantage < -moveSpeed)
            {
                var punishableMove = new PunishableMove
                {
                    Move = move,
                    PunishWindow = Math.Abs(move.BlockAdvantage) - moveSpeed,
                    RecommendedPunishes = frameData.StandingNormals
                        .Where(n => n.StartupFrames <= Math.Abs(move.BlockAdvantage) - 2)
                        .OrderByDescending(n => n.Damage)
                        .Take(3)
                        .ToList()
                };
                
                punishable.Add(punishableMove);
            }
        }
        
        return punishable.OrderByDescending(p => p.PunishWindow).ToList();
    }

    /// <summary>
    /// Generates a matchup analysis between two characters.
    /// </summary>
    public MatchupAnalysis AnalyzeMatchup(CharacterFrameData char1, CharacterFrameData char2)
    {
        return new MatchupAnalysis
        {
            Character1 = char1.CharacterName,
            Character2 = char2.CharacterName,
            Char1Punishes = FindPunishableMoves(char2), // What char1 can punish
            Char2Punishes = FindPunishableMoves(char1), // What char2 can punish
            SpeedComparison = char1.WalkSpeed.CompareTo(char2.WalkSpeed),
            HealthDifference = char1.Health - char2.Health,
            Notes = GenerateMatchupNotes(char1, char2)
        };
    }

    private string GenerateMatchupNotes(CharacterFrameData char1, CharacterFrameData char2)
    {
        var notes = new List<string>();
        
        if (char1.WalkSpeed > char2.WalkSpeed)
            notes.Add($"{char1.CharacterName} is faster - use mobility advantage");
        
        if (char1.Health > char2.Health)
            notes.Add($"{char1.CharacterName} has more health ({char1.Health} vs {char2.Health})");
        
        // Check for fireball wars
        var char1HasProjectile = char1.SpecialMoves.Any(m => m.IsProjectile);
        var char2HasProjectile = char2.SpecialMoves.Any(m => m.IsProjectile);
        
        if (char1HasProjectile && !char2HasProjectile)
            notes.Add($"{char1.CharacterName} has projectile advantage");
        else if (!char1HasProjectile && char2HasProjectile)
            notes.Add($"{char2.CharacterName} has projectile advantage - consider anti-airs");
        else if (char1HasProjectile && char2HasProjectile)
            notes.Add("Fireball war likely - watch for jump-ins");
        
        return string.Join("\n", notes);
    }
}

/// <summary>
/// CLSN box definition.
/// </summary>
public class ClsnBox
{
    public ClsnType Type { get; set; }
    public int Index { get; set; }
    public int X1 { get; set; }
    public int Y1 { get; set; }
    public int X2 { get; set; }
    public int Y2 { get; set; }
}

public enum ClsnType
{
    Hitbox,   // Red - deals damage
    Hurtbox,  // Blue - can be hit
    Collision // Green - pushes other characters
}

/// <summary>
/// Represents a move that can be punished on block.
/// </summary>
public class PunishableMove
{
    public required MoveFrameData Move { get; set; }
    public int PunishWindow { get; set; }
    public List<MoveFrameData> RecommendedPunishes { get; set; } = new();
}

/// <summary>
/// Matchup analysis between two characters.
/// </summary>
public class MatchupAnalysis
{
    public string Character1 { get; set; } = string.Empty;
    public string Character2 { get; set; } = string.Empty;
    public List<PunishableMove> Char1Punishes { get; set; } = new();
    public List<PunishableMove> Char2Punishes { get; set; } = new();
    public int SpeedComparison { get; set; }
    public int HealthDifference { get; set; }
    public string Notes { get; set; } = string.Empty;
}
