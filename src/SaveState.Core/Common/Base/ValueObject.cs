namespace SaveState.Core.Common.Base;

using SaveState.Core.Common.Interfaces;

public abstract class ValueObject : IValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    IEnumerable<object> IValueObject.GetEqualityComponents() => GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return (GetEqualityComponents() ?? Enumerable.Empty<object>())
            .SequenceEqual(other.GetEqualityComponents() ?? Enumerable.Empty<object>());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
}
