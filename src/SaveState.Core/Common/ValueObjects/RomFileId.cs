using SaveState.Core.Common.Base;

namespace SaveState.Core.Common.ValueObjects;

public sealed class RomFileId : ValueObject
{
    public Guid Value { get; }

    private RomFileId(Guid value)
    {
        Value = value;
    }

    public static RomFileId NewId() => new(Guid.NewGuid());
    public static RomFileId From(Guid value) => new(value);

    public static implicit operator Guid(RomFileId id) => id.Value;
    public static explicit operator RomFileId(Guid value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
