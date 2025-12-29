using SaveState.Core.Common.Base;

namespace SaveState.Core.Common.ValueObjects;

public sealed class BackupId : ValueObject
{
    public Guid Value { get; }

    private BackupId(Guid value)
    {
        Value = value;
    }

    public static BackupId NewId() => new(Guid.NewGuid());
    public static BackupId From(Guid value) => new(value);

    public static implicit operator Guid(BackupId id) => id.Value;
    public static explicit operator BackupId(Guid value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
