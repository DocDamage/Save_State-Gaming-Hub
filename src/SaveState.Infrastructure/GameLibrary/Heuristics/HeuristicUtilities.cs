using System.Globalization;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Provides shared utility methods for value heuristics used in game memory auto-discovery.
/// </summary>
/// <remarks>
/// This static class contains common conversion and validation methods used across
/// multiple heuristic implementations to ensure consistent behavior and reduce code duplication.
/// </remarks>
public static class HeuristicUtilities
{
    /// <summary>
    /// Converts an object value to a nullable double using invariant culture formatting.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>
    /// The converted double value if conversion succeeds; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// Uses <see cref="CultureInfo.InvariantCulture"/> to ensure consistent parsing
    /// regardless of the current thread culture. Supports numeric types and string representations.
    /// </remarks>
    public static double? ConvertToDouble(object value)
    {
        if (value is double d)
            return d;

        if (value is float f)
            return f;

        if (value is int i)
            return i;

        if (value is long l)
            return l;

        if (value is decimal dec)
            return (double)dec;

        if (value is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;
    }

    /// <summary>
    /// Determines whether the specified value is an integer type.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>
    /// <c>true</c> if the value is an integer type (byte, short, int, long, or their unsigned variants);
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method checks the runtime type of the value against known integer types.
    /// Floating-point types and non-numeric types return <c>false</c>.
    /// </remarks>
    public static bool IsIntegerValue(object? value)
    {
        return value is byte or sbyte or short or ushort or int or uint or long or ulong;
    }

    /// <summary>
    /// Checks whether the specified value falls within the specified inclusive range.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum acceptable value (inclusive).</param>
    /// <param name="max">The maximum acceptable value (inclusive).</param>
    /// <returns>
    /// <c>true</c> if the value is a numeric type and falls within the specified range;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Non-numeric values and null values return <c>false</c>.
    /// The comparison is performed using double precision for maximum compatibility.
    /// </remarks>
    public static bool IsInRange(object? value, double min, double max)
    {
        if (value == null)
            return false;

        var doubleValue = ConvertToDouble(value);
        if (!doubleValue.HasValue)
            return false;

        return doubleValue.Value >= min && doubleValue.Value <= max;
    }
}
