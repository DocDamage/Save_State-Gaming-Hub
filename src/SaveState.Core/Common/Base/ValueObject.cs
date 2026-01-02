namespace SaveState.Core.Common.Base;

using SaveState.Core.Common.Interfaces;

/// <summary>
/// Base class for value objects. Value objects are compared by their properties,
/// not by identity. Override <see cref="GetEqualityComponents"/> to define equality.
/// </summary>
public abstract class ValueObject : IValueObject
{
    /// <summary>
    /// Gets the components used for equality comparison.
    /// Override this to return all properties that define equality.
    /// </summary>
    protected abstract IEnumerable<object> GetEqualityComponents();

    /// <inheritdoc/>
    IEnumerable<object> IValueObject.GetEqualityComponents() => GetEqualityComponents();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return (GetEqualityComponents() ?? Enumerable.Empty<object>())
            .SequenceEqual(other.GetEqualityComponents() ?? Enumerable.Empty<object>());
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
}

