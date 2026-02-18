using SaveState.Core.Common;

namespace SaveState.Core.Mugen.CharacterFrameAnalysis.Services;

/// <summary>
/// Service for managing character frame data.
/// </summary>
public interface IFrameDataService
{
    /// <summary>
    /// Loads or parses frame data for a character.
    /// </summary>
    Task<Result<CharacterFrameData>> LoadFrameDataAsync(string characterPath, CancellationToken ct = default);
    
    /// <summary>
    /// Gets cached frame data for a character.
    /// </summary>
    Task<Result<CharacterFrameData>> GetFrameDataAsync(string characterName, CancellationToken ct = default);
    
    /// <summary>
    /// Saves frame data to cache/database.
    /// </summary>
    Task<Result> SaveFrameDataAsync(CharacterFrameData frameData, CancellationToken ct = default);
    
    /// <summary>
    /// Analyzes matchup between two characters.
    /// </summary>
    Task<Result<MatchupAnalysis>> AnalyzeMatchupAsync(string char1Name, string char2Name, CancellationToken ct = default);
    
    /// <summary>
    /// Gets punishable moves for a character.
    /// </summary>
    Task<Result<List<PunishableMove>>> GetPunishableMovesAsync(string characterName, int playerSpeed = 5, CancellationToken ct = default);
    
    /// <summary>
    /// Compares two moves side by side.
    /// </summary>
    Task<Result<MoveComparison>> CompareMovesAsync(string char1Name, string move1Name, string char2Name, string move2Name, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all characters with frame data.
    /// </summary>
    Task<Result<List<string>>> GetCharactersWithFrameDataAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Refreshes frame data by re-parsing character files.
    /// </summary>
    Task<Result<CharacterFrameData>> RefreshFrameDataAsync(string characterPath, CancellationToken ct = default);
}

/// <summary>
/// Comparison result between two moves.
/// </summary>
public class MoveComparison
{
    public MoveFrameData Move1 { get; set; } = null!;
    public MoveFrameData Move2 { get; set; } = null!;
    public int SpeedDifference => Move1.StartupFrames - Move2.StartupFrames;
    public int DamageDifference => Move1.Damage - Move2.Damage;
    public int AdvantageDifference => Move1.BlockAdvantage - Move2.BlockAdvantage;
    public string Winner => DetermineWinner();
    public List<string> Advantages1 { get; set; } = new();
    public List<string> Advantages2 { get; set; } = new();
    
    private string DetermineWinner()
    {
        var score1 = 0;
        var score2 = 0;
        
        if (Move1.StartupFrames < Move2.StartupFrames) score1++;
        else if (Move2.StartupFrames < Move1.StartupFrames) score2++;
        
        if (Move1.Damage > Move2.Damage) score1++;
        else if (Move2.Damage > Move1.Damage) score2++;
        
        if (Move1.BlockAdvantage > Move2.BlockAdvantage) score1++;
        else if (Move2.BlockAdvantage > Move1.BlockAdvantage) score2++;
        
        return score1 > score2 ? Move1.MoveName : score2 > score1 ? Move2.MoveName : "Tie";
    }
}
