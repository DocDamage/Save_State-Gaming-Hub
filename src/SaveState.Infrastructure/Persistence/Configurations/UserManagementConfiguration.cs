using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveState.Core.UserManagement.Entities;
using UserManagementUser = SaveState.Core.UserManagement.Entities.User;
using UserManagementRole = SaveState.Core.UserManagement.Entities.Role;
using UserManagementUserRole = SaveState.Core.UserManagement.Entities.UserRole;
using UserManagementPermission = SaveState.Core.UserManagement.Entities.Permission;
using UserManagementRolePermission = SaveState.Core.UserManagement.Entities.RolePermission;
using UserManagementApiKey = SaveState.Core.UserManagement.Entities.ApiKey;

namespace SaveState.Infrastructure.Persistence.Configurations;

public class UserManagementConfiguration :
    IEntityTypeConfiguration<UserManagementUser>,
    IEntityTypeConfiguration<UserManagementRole>,
    IEntityTypeConfiguration<UserManagementUserRole>,
    IEntityTypeConfiguration<UserManagementPermission>,
    IEntityTypeConfiguration<UserManagementRolePermission>,
    IEntityTypeConfiguration<UserManagementApiKey>
{
    public void Configure(EntityTypeBuilder<UserManagementUser> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.PasswordSalt)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.IsEmailVerified)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        builder.HasIndex(u => u.LastLoginAt)
            .HasDatabaseName("IX_Users_LastLoginAt");
    }

    public void Configure(EntityTypeBuilder<UserManagementRole> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.IsSystemRole)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("IX_Roles_Name");

        builder.HasIndex(r => r.IsSystemRole)
            .HasDatabaseName("IX_Roles_IsSystemRole");
    }

    public void Configure(EntityTypeBuilder<UserManagementUserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ur => ur.UserId)
            .HasDatabaseName("IX_UserRoles_UserId");

        builder.HasIndex(ur => ur.RoleId)
            .HasDatabaseName("IX_UserRoles_RoleId");
    }

    public void Configure(EntityTypeBuilder<UserManagementPermission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.Resource)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Action)
            .IsRequired()
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(p => new { p.Resource, p.Action })
            .IsUnique()
            .HasDatabaseName("IX_Permissions_Resource_Action");

        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("IX_Permissions_Name");
    }

    public void Configure(EntityTypeBuilder<UserManagementRolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(rp => rp.RoleId)
            .HasDatabaseName("IX_RolePermissions_RoleId");

        builder.HasIndex(rp => rp.PermissionId)
            .HasDatabaseName("IX_RolePermissions_PermissionId");
    }

    public void Configure(EntityTypeBuilder<UserManagementApiKey> builder)
    {
        builder.ToTable("ApiKeys");

        builder.HasKey(ak => ak.Id);

        builder.Property(ak => ak.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ak => ak.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ak => ak.KeyHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(ak => ak.KeyPrefix)
            .IsRequired()
            .HasMaxLength(8);

        builder.Property(ak => ak.CreatedAt)
            .IsRequired();

        builder.Property(ak => ak.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(ak => ak.User)
            .WithMany(u => u.ApiKeys)
            .HasForeignKey(ak => ak.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ak => ak.UserId)
            .HasDatabaseName("IX_ApiKeys_UserId");

        builder.HasIndex(ak => ak.KeyPrefix)
            .HasDatabaseName("IX_ApiKeys_KeyPrefix");

        builder.HasIndex(ak => ak.IsActive)
            .HasDatabaseName("IX_ApiKeys_IsActive");

        builder.HasIndex(ak => ak.LastUsedAt)
            .HasDatabaseName("IX_ApiKeys_LastUsedAt");
    }
}
