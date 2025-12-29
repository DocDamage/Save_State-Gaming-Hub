using SaveState.Core.Common.Base;

namespace SaveState.Core.RomManagement.ValueObjects;

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
    public bool Exists() => File.Exists(Value);

    public static implicit operator string(FilePath path) => path.Value;
    public static explicit operator FilePath(string value) => new(value);

    public override string ToString() => Value;
}
