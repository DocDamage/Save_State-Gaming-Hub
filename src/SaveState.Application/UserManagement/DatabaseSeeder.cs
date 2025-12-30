using SaveState.Core.UserManagement.Entities;
using SaveState.Core.UserManagement.Repositories;
using SaveState.Core.UserManagement.Services;

namespace SaveState.Application.UserManagement;

/// <summary>
/// Service to seed the database with default roles and permissions.
/// </summary>
public class DatabaseSeeder
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public DatabaseSeeder(
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedRolesAsync(ct);
        await SeedAdminUserAsync(ct);
    }

    private static async Task SeedPermissionsAsync(CancellationToken ct)
    {
        // This would create permissions in a real implementation
        // For now, we'll work with role-based permissions only
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        var roles = new[]
        {
            ("Admin", "Administrator with full system access"),
            ("Moderator", "Moderator with content management access"),
            ("User", "Standard user with basic access"),
            ("Guest", "Limited access for unauthenticated users")
        };

        foreach (var (name, description) in roles)
        {
            var existingRole = await _roleRepository.GetByNameAsync(name, ct);
            if (existingRole == null)
            {
                var role = Role.Create(name, description, name == "Admin" || name == "User");
                await _roleRepository.AddAsync(role, ct);
            }
        }
    }

    private async Task SeedAdminUserAsync(CancellationToken ct)
    {
        const string adminUsername = "admin";
        const string adminEmail = "admin@savestate.local";
        const string adminPassword = "Admin123!";

        var existingUser = await _userRepository.GetByUsernameAsync(adminUsername, ct);
        if (existingUser != null)
            return;

        var (passwordHash, passwordSalt) = _passwordHasher.HashPassword(adminPassword);
        var adminUser = User.Create(adminUsername, adminEmail, passwordHash, passwordSalt);

        // Assign admin role
        var adminRole = await _roleRepository.GetByNameAsync("Admin", ct);
        if (adminRole != null)
        {
            adminUser.AddRole(adminRole);
        }

        await _userRepository.AddAsync(adminUser, ct);
    }
}
