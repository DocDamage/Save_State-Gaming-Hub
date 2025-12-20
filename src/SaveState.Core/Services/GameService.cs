using Microsoft.EntityFrameworkCore;
using SaveState.Core.Data;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;

namespace SaveState.Core.Services;

public class GameService : IGameService
{
    private readonly SaveStateDbContext _context;
    private readonly ILogger _logger;

    public GameService(SaveStateDbContext context)
    {
        _context = context;
        _logger = Log.ForContext<GameService>();
    }

    public async Task<IEnumerable<Game>> GetAllAsync()
    {
        return await _context.Games
            .Include(g => g.Platform)
            .ToListAsync();
    }

    public async Task<Game?> GetByIdAsync(Guid id)
    {
        return await _context.Games
            .Include(g => g.Platform)
            .Include(g => g.Images)
            .Include(g => g.Achievements)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task AddAsync(Game game)
    {
        _logger.Information("Adding new game: {Title}", game.Title);
        await _context.Games.AddAsync(game);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Game game)
    {
        _logger.Information("Updating game: {Title} ({Id})", game.Title, game.Id);
        _context.Games.Update(game);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.Warning("Deleting game with ID: {Id}", id);
        var game = await _context.Games.FindAsync(id);
        if (game != null)
        {
            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Game>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync();

        return await _context.Games
            .Include(g => g.Platform)
            .Where(g => g.Title.Contains(query) || 
                        (g.SortTitle != null && g.SortTitle.Contains(query)))
            .ToListAsync();
    }
}
