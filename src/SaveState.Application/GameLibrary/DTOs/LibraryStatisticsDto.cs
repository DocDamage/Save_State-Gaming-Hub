using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Application.GameLibrary.DTOs;

public class LibraryStatisticsDto
{
    public int TotalGames { get; set; }
    public int InstalledGames { get; set; }
    public int RunningGames { get; set; }
    public Dictionary<GameStatus, int> GamesByStatus { get; set; } = new();
    public Dictionary<string, int> GamesByPlatform { get; set; } = new();
    public long TotalDiskSpaceUsed { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
    public DateTime? LastGameAdded { get; set; }
}
