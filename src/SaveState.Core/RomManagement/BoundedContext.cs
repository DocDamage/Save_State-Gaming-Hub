namespace SaveState.Core.RomManagement;

using SaveState.Core.Common.Base;
using SaveState.Core.RomManagement.Entities;

/// <summary>
/// RomManagement Bounded Context - handles ROM file discovery, scanning, emulator management, and BIOS handling.
/// This context owns entities related to ROM files, emulators, BIOS files, and ROM metadata.
/// </summary>
public static class RomManagementContext
{
    public const string Name = "RomManagement";

    // Entities owned by this context
    public static readonly Type[] Entities = {
        typeof(RomFile),
        typeof(Emulator),
        typeof(BiosFile),
        typeof(RomMetadata)
    };

    // Domain services
    public interface IRomScannerService { /* Implementation in Application layer */ }
    public interface IEmulatorManagerService { /* Implementation in Application layer */ }
    public interface IBiosManagerService { /* Implementation in Application layer */ }

    // Value objects for this context
    public class FilePath : ValueObject
    {
        public string Value { get; }

        public FilePath(string value)
        {
            Value = Guard.Against.NullOrWhiteSpace(value, nameof(value));
            if (!Path.IsPathRooted(Value))
                throw new ArgumentException("Path must be absolute", nameof(value));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value.ToLowerInvariant();
        }

        public string GetDirectory() => Path.GetDirectoryName(Value)!;
        public string GetFileName() => Path.GetFileName(Value);
        public string GetExtension() => Path.GetExtension(Value);
        public string GetFileNameWithoutExtension() => Path.GetFileNameWithoutExtension(Value);

        public static implicit operator string(FilePath path) => path.Value;
        public static explicit operator FilePath(string value) => new(value);
    }

    public class FileSize : ValueObject
    {
        public long Bytes { get; }

        public FileSize(long bytes)
        {
            if (bytes < 0)
                throw new ArgumentException("File size cannot be negative", nameof(bytes));
            Bytes = bytes;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Bytes;
        }

        public string ToHumanReadable()
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (Bytes >= GB) return $"{Bytes / (double)GB:F2} GB";
            if (Bytes >= MB) return $"{Bytes / (double)MB:F2} MB";
            if (Bytes >= KB) return $"{Bytes / (double)KB:F2} KB";
            return $"{Bytes} bytes";
        }

        public static implicit operator long(FileSize size) => size.Bytes;
        public static explicit operator FileSize(long bytes) => new(bytes);
    }

    public class EmulatorPath : ValueObject
    {
        public string Value { get; }

        public EmulatorPath(string value)
        {
            Value = Guard.Against.NullOrWhiteSpace(value, nameof(value));
            if (!File.Exists(Value))
                throw new ArgumentException("Emulator executable must exist", nameof(value));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value.ToLowerInvariant();
        }

        public string GetDirectory() => Path.GetDirectoryName(Value)!;
        public string GetExecutableName() => Path.GetFileName(Value);

        public static implicit operator string(EmulatorPath path) => path.Value;
        public static explicit operator EmulatorPath(string value) => new(value);
    }
}

// RomFile entity is now fully implemented in Entities/RomFile.cs

public class Emulator : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public RomManagementContext.EmulatorPath ExecutablePath { get; private set; } = null!;
    public string Platform { get; private set; } = string.Empty;
    public string? Version { get; private set; }
    public Dictionary<string, string> DefaultArguments { get; private set; } = new();

    private Emulator() { }

    public static Emulator Create(string name, RomManagementContext.EmulatorPath executablePath, string platform, string? version = null)
    {
        return new Emulator
        {
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            ExecutablePath = Guard.Against.Null(executablePath, nameof(executablePath)),
            Platform = Guard.Against.NullOrWhiteSpace(platform, nameof(platform)),
            Version = version,
            DefaultArguments = new Dictionary<string, string>()
        };
    }
}

public class BiosFile : EntityBase
{
    public RomManagementContext.FilePath FilePath { get; private set; } = null!;
    public RomManagementContext.FileSize FileSize { get; private set; }
    public string Platform { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsRequired { get; private set; }

    private BiosFile() { }

    public static BiosFile Create(RomManagementContext.FilePath filePath, RomManagementContext.FileSize fileSize, string platform, string? description = null, bool isRequired = false)
    {
        return new BiosFile
        {
            FilePath = Guard.Against.Null(filePath, nameof(filePath)),
            FileSize = Guard.Against.Null(fileSize, nameof(fileSize)),
            Platform = Guard.Against.NullOrWhiteSpace(platform, nameof(platform)),
            Description = description,
            IsRequired = isRequired
        };
    }
}

public class RomMetadata : EntityBase
{
    public Guid RomFileId { get; private set; }
    public string? Title { get; private set; }
    public string? Region { get; private set; }
    public string? Language { get; private set; }
    public string? Genre { get; private set; }
    public DateTime? ReleaseDate { get; private set; }
    public Dictionary<string, string> AdditionalMetadata { get; private set; } = new();

    private RomMetadata() { }

    public static RomMetadata Create(Guid romFileId, string? title = null, string? region = null)
    {
        return new RomMetadata
        {
            RomFileId = Guard.Against.Default(romFileId, nameof(romFileId)),
            Title = title,
            Region = region,
            AdditionalMetadata = new Dictionary<string, string>()
        };
    }
}
