using SaveState.Core.Common.Base;

namespace SaveState.Core.Common.ValueObjects;

public sealed class GameTitle : ValueObject
{
    public string Value { get; }

    private GameTitle(string value)
    {
        Value = Guard.Against.NullOrWhiteSpace(value, "title")
            .Trim();
        if (Value.Length < 1 || Value.Length > 200)
            throw new ArgumentException("Game title must be 1-200 characters", "title");
    }

    public static GameTitle From(string value) => new(value);

    public static implicit operator string(GameTitle title) => title.Value;
    public static explicit operator GameTitle(string value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant(); // Case-insensitive comparison
    }

    public override string ToString() => Value;
}
