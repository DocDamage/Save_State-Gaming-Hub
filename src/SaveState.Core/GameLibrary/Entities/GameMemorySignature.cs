using System.Text.Json.Serialization;

namespace SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Represents a memory signature for detecting game values in process memory.
/// Used by the GameMemoryReader to locate and read dynamic memory addresses.
/// </summary>
public class GameMemorySignature
{
    /// <summary>
    /// The game title this signature applies to. Use "*" for universal patterns.
    /// </summary>
    [JsonPropertyName("gameTitle")]
    public string GameTitle { get; set; } = "";

    /// <summary>
    /// The name of the value (e.g., "Health", "Score", "Lives").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// Hex pattern to search for (e.g., "A1 ?? ?? ?? ?? 8B"). Use ?? for wildcards.
    /// </summary>
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = "";

    /// <summary>
    /// Offset from the pattern match to the actual value.
    /// </summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>
    /// Data type of the value: int32, int64, float, double, byte, int16.
    /// </summary>
    [JsonPropertyName("valueType")]
    public string ValueType { get; set; } = "int32";

    /// <summary>
    /// Optional description of what this signature represents.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Minimum expected value for validation (optional).
    /// </summary>
    [JsonPropertyName("minValue")]
    public long? MinValue { get; set; }

    /// <summary>
    /// Maximum expected value for validation (optional).
    /// </summary>
    [JsonPropertyName("maxValue")]
    public long? MaxValue { get; set; }

    /// <summary>
    /// For float/double values: minimum expected value (optional).
    /// </summary>
    [JsonPropertyName("minFloatValue")]
    public double? MinFloatValue { get; set; }

    /// <summary>
    /// For float/double values: maximum expected value (optional).
    /// </summary>
    [JsonPropertyName("maxFloatValue")]
    public double? MaxFloatValue { get; set; }

    /// <summary>
    /// Module name to search within (e.g., "game.exe"). Null means search all modules.
    /// </summary>
    [JsonPropertyName("moduleName")]
    public string? ModuleName { get; set; }

    /// <summary>
    /// Priority for scanning (higher = scanned first). Default is 0.
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    /// <summary>
    /// Whether this signature is enabled for scanning.
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Tags for categorizing signatures (e.g., "critical", "cosmetic").
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// When this signature was added.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version of the game this signature works with (optional).
    /// </summary>
    [JsonPropertyName("gameVersion")]
    public string? GameVersion { get; set; }

    /// <summary>
    /// Validates that the read value is within expected bounds.
    /// </summary>
    public bool IsValidValue(object? value)
    {
        if (value == null) return false;

        return value switch
        {
            int intValue => ValidateInt(intValue),
            long longValue => ValidateLong(longValue),
            float floatValue => ValidateFloat(floatValue),
            double doubleValue => ValidateDouble(doubleValue),
            byte byteValue => ValidateInt(byteValue),
            short shortValue => ValidateInt(shortValue),
            _ => true
        };
    }

    private bool ValidateInt(int value)
    {
        if (MinValue.HasValue && value < MinValue.Value) return false;
        if (MaxValue.HasValue && value > MaxValue.Value) return false;
        return true;
    }

    private bool ValidateLong(long value)
    {
        if (MinValue.HasValue && value < MinValue.Value) return false;
        if (MaxValue.HasValue && value > MaxValue.Value) return false;
        return true;
    }

    private bool ValidateFloat(float value)
    {
        if (MinFloatValue.HasValue && value < MinFloatValue.Value) return false;
        if (MaxFloatValue.HasValue && value > MaxFloatValue.Value) return false;
        return true;
    }

    private bool ValidateDouble(double value)
    {
        if (MinFloatValue.HasValue && value < MinFloatValue.Value) return false;
        if (MaxFloatValue.HasValue && value > MaxFloatValue.Value) return false;
        return true;
    }

    /// <summary>
    /// Gets the size in bytes for the value type.
    /// </summary>
    public int GetValueSize()
    {
        return ValueType.ToLowerInvariant() switch
        {
            "int8" or "byte" => 1,
            "int16" or "short" => 2,
            "int32" or "int" => 4,
            "int64" or "long" => 8,
            "float" => 4,
            "double" => 8,
            "bool" => 1,
            _ => 4 // Default to int32
        };
    }

    /// <summary>
    /// Returns a string representation of the signature.
    /// </summary>
    public override string ToString() => $"{GameTitle}/{Name}: {Description ?? Pattern}";
}
