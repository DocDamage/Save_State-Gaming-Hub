using SaveState.Core.Entities;

namespace SaveState.Core.Interfaces;

public interface IGameService
{
    Task<IEnumerable<Game>> GetAllAsync();
    Task<Game?> GetByIdAsync(Guid id);
    Task AddAsync(Game game);
    Task UpdateAsync(Game game);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Game>> SearchAsync(string query);
}
