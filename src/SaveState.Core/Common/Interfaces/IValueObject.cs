namespace SaveState.Core.Common.Interfaces;

public interface IValueObject
{
    IEnumerable<object> GetEqualityComponents();
}
