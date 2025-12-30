using Microsoft.EntityFrameworkCore;
using SaveState.Core.UserManagement.Entities;
using SaveState.Core.UserManagement.Repositories;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.UserManagement;

public class RoleRepository : IRoleRepository
{
    private readonly SaveStateDbContext _context;

    public RoleRepository(SaveStateDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Set<Role>()
            .Include(r => r.UserRoles)
                .ThenInclude(ur => ur.User)
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be null or whitespace.", nameof(name));

        return await _context.Set<Role>()
            .Include(r => r.UserRoles)
                .ThenInclude(ur => ur.User)
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Name == name, ct);
    }

    public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Set<Role>()
            .Include(r => r.UserRoles)
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Role>> GetSystemRolesAsync(CancellationToken ct = default)
    {
        return await _context.Set<Role>()
            .Include(r => r.UserRoles)
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => r.IsSystemRole)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Role role, CancellationToken ct = default)
    {
        if (role == null)
            throw new ArgumentNullException(nameof(role));

        await _context.Set<Role>().AddAsync(role, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        if (role == null)
            throw new ArgumentNullException(nameof(role));

        _context.Set<Role>().Update(role);
        await _context.SaveChangesAsync(ct);
    }
}
