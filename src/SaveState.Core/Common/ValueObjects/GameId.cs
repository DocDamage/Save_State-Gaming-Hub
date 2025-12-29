using SaveState.Core.Common.Base;

namespace SaveState.Core.Common.ValueObjects;

public sealed class GameId : ValueObject
{
    public Guid Value { get; }

    private GameId(Guid value)
    {
        Value = value;
    }

    public static GameId NewId() => new(Guid.NewGuid());
    public static GameId From(Guid value) => new(value);

    public static implicit operator Guid(GameId id) => id.Value;
    public static explicit operator GameId(Guid value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
