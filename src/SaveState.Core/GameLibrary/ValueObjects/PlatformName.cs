using SaveState.Core.Common.Base;

namespace SaveState.Core.GameLibrary.ValueObjects;

public class PlatformName : ValueObject
{
    public string Value { get; }

    private PlatformName(string value)
    {
        Value = Guard.Against.NullOrWhiteSpace(value, nameof(value))
            .Trim();
        if (Value.Length < 1 || Value.Length > 100)
            throw new ArgumentException("Platform name must be 1-100 characters", nameof(value));
    }

    public static PlatformName From(string value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public static implicit operator string(PlatformName name) => name.Value;
    public static explicit operator PlatformName(string value) => new(value);

    public override string ToString() => Value;
}
