using SaveState.Core.Models;

namespace SaveState.Core.Interfaces;

public interface ITrainerService
{
    Task<List<TrainerDefinition>> GetAllTrainersAsync();
    Task CreateCheatAsync(string processName, string cheatName, string address, string type, string defaultValue, bool isFreeze);
    Task ToggleCheatAsync(CheatDefinition cheat, bool isActive);
}
