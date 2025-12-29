using SaveState.Core.Common.Base;
using SaveState.Core.RomManagement.ValueObjects;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.RomManagement.Entities;

public class Emulator : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public FilePath ExecutablePath { get; private set; } = null!;
    public string? Version { get; private set; }
    public string? Description { get; private set; }
    public Guid PlatformId { get; private set; }
    public Platform Platform { get; private set; } = null!;
    public string? CommandLineArgs { get; private set; }
    public bool IsAvailable { get; private set; }

    protected Emulator() { } // EF Core

    public Emulator(string name, FilePath executablePath, Guid platformId)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        ExecutablePath = Guard.Against.Null(executablePath, nameof(executablePath));
        PlatformId = platformId;
        IsAvailable = executablePath.Exists();
    }

    public void UpdateVersion(string version)
    {
        Version = Guard.Against.NullOrWhiteSpace(version, nameof(version));
    }

    public void UpdateDescription(string description)
    {
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
    }

    public void SetCommandLineArgs(string args)
    {
        CommandLineArgs = args;
    }

    public void CheckAvailability()
    {
        IsAvailable = ExecutablePath.Exists();
    }
}
