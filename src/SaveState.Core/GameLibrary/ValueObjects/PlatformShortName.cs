using SaveState.Core.Common.Base;

namespace SaveState.Core.GameLibrary.ValueObjects;

public class PlatformShortName : ValueObject
{
    public string Value { get; }

    private PlatformShortName(string value)
    {
        Value = Guard.Against.NullOrWhiteSpace(value, nameof(value))
            .Trim()
            .ToUpperInvariant();
        if (Value.Length < 1 || Value.Length > 20)
            throw new ArgumentException("Platform short name must be 1-20 characters", nameof(value));
        if (!System.Text.RegularExpressions.Regex.IsMatch(Value, "^[A-Z0-9_-]+$"))
            throw new ArgumentException("Platform short name can only contain letters, numbers, underscores, and hyphens", nameof(value));
    }

    public static PlatformShortName From(string value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(PlatformShortName shortName) => shortName.Value;
    public static explicit operator PlatformShortName(string value) => new(value);

    public override string ToString() => Value;
}
