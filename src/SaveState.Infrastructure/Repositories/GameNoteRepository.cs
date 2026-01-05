namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;

public class GameNoteRepository : IGameNoteRepository
{
    private readonly SaveStateDbContext _context;

    public GameNoteRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GameNote>> GetByGameIdAsync(GameId gameId, UserId userId, CancellationToken ct = default)
    {
        return await _context.GameNotes
            .Where(n => n.GameId == gameId && n.UserId == userId)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task<GameNote?> GetByIdAsync(Guid noteId, CancellationToken ct = default)
    {
        return await _context.GameNotes
            .FirstOrDefaultAsync(n => n.Id == noteId, ct);
    }

    public async Task<GameNote> AddAsync(GameNote note, CancellationToken ct = default)
    {
        await _context.GameNotes.AddAsync(note, ct);
        await _context.SaveChangesAsync(ct);
        return note;
    }

    public async Task UpdateAsync(GameNote note, CancellationToken ct = default)
    {
        _context.GameNotes.Update(note);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid noteId, CancellationToken ct = default)
    {
        var note = await GetByIdAsync(noteId, ct);
        if (note != null)
        {
            _context.GameNotes.Remove(note);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<GameNote>> SearchAsync(UserId userId, string searchTerm, CancellationToken ct = default)
    {
        return await _context.GameNotes
            .Where(n => n.UserId == userId &&
                       (n.Title.Contains(searchTerm) || n.Content.Contains(searchTerm)))
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync(ct);
    }
}
