namespace SaveState.Core.Performance.ValueObjects;

/// <summary>
/// Supported data types for memory reading and scanning.
/// </summary>
public enum MemoryDataType
{
    /// <summary>8-bit signed integer (-128 to 127)</summary>
    Int8,

    /// <summary>8-bit unsigned integer (0 to 255)</summary>
    UInt8,

    /// <summary>16-bit signed integer (-32,768 to 32,767)</summary>
    Int16,

    /// <summary>16-bit unsigned integer (0 to 65,535)</summary>
    UInt16,

    /// <summary>32-bit signed integer (-2.1B to 2.1B)</summary>
    Int32,

    /// <summary>32-bit unsigned integer (0 to 4.2B)</summary>
    UInt32,

    /// <summary>64-bit signed integer</summary>
    Int64,

    /// <summary>64-bit unsigned integer</summary>
    UInt64,

    /// <summary>32-bit floating point</summary>
    Float,

    /// <summary>64-bit floating point</summary>
    Double,

    /// <summary>UTF-8 string (null-terminated)</summary>
    String,

    /// <summary>Raw byte array</summary>
    ByteArray
}

/// <summary>
/// Extension methods for MemoryDataType.
/// </summary>
public static class MemoryDataTypeExtensions
{
    /// <summary>
    /// Gets the size in bytes for the data type.
    /// </summary>
    public static int GetSize(this MemoryDataType type)
    {
        return type switch
        {
            MemoryDataType.Int8 => 1,
            MemoryDataType.UInt8 => 1,
            MemoryDataType.Int16 => 2,
            MemoryDataType.UInt16 => 2,
            MemoryDataType.Int32 => 4,
            MemoryDataType.UInt32 => 4,
            MemoryDataType.Int64 => 8,
            MemoryDataType.UInt64 => 8,
            MemoryDataType.Float => 4,
            MemoryDataType.Double => 8,
            MemoryDataType.String => -1, // Variable
            MemoryDataType.ByteArray => -1, // Variable
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown data type")
        };
    }

    /// <summary>
    /// Converts raw bytes to the specified data type.
    /// </summary>
    public static object? ParseValue(this MemoryDataType type, byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return null;

        return type switch
        {
            MemoryDataType.Int8 => (sbyte)bytes[0],
            MemoryDataType.UInt8 => bytes[0],
            MemoryDataType.Int16 => BitConverter.ToInt16(bytes, 0),
            MemoryDataType.UInt16 => BitConverter.ToUInt16(bytes, 0),
            MemoryDataType.Int32 => BitConverter.ToInt32(bytes, 0),
            MemoryDataType.UInt32 => BitConverter.ToUInt32(bytes, 0),
            MemoryDataType.Int64 => BitConverter.ToInt64(bytes, 0),
            MemoryDataType.UInt64 => BitConverter.ToUInt64(bytes, 0),
            MemoryDataType.Float => BitConverter.ToSingle(bytes, 0),
            MemoryDataType.Double => BitConverter.ToDouble(bytes, 0),
            MemoryDataType.String => System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0'),
            MemoryDataType.ByteArray => bytes,
            _ => null
        };
    }

    /// <summary>
    /// Converts an object value to its raw byte representation.
    /// </summary>
    public static byte[] ToBytes(this MemoryDataType type, object value)
    {
        return type switch
        {
            MemoryDataType.Int8 => new[] { (byte)Convert.ToSByte(value) },
            MemoryDataType.UInt8 => new[] { Convert.ToByte(value) },
            MemoryDataType.Int16 => BitConverter.GetBytes(Convert.ToInt16(value)),
            MemoryDataType.UInt16 => BitConverter.GetBytes(Convert.ToUInt16(value)),
            MemoryDataType.Int32 => BitConverter.GetBytes(Convert.ToInt32(value)),
            MemoryDataType.UInt32 => BitConverter.GetBytes(Convert.ToUInt32(value)),
            MemoryDataType.Int64 => BitConverter.GetBytes(Convert.ToInt64(value)),
            MemoryDataType.UInt64 => BitConverter.GetBytes(Convert.ToUInt64(value)),
            MemoryDataType.Float => BitConverter.GetBytes(Convert.ToSingle(value)),
            MemoryDataType.Double => BitConverter.GetBytes(Convert.ToDouble(value)),
            MemoryDataType.String => System.Text.Encoding.UTF8.GetBytes(value.ToString() + "\0"),
            MemoryDataType.ByteArray => value is string s ? Convert.FromHexString(s) : (byte[])value,
            _ => Array.Empty<byte>()
        };
    }

    /// <summary>
    /// Parses a string representation of a value into the correct object type.
    /// </summary>
    public static object? ParseValueFromString(this MemoryDataType type, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        try
        {
            return type switch
            {
                MemoryDataType.Int8 => sbyte.Parse(value),
                MemoryDataType.UInt8 => byte.Parse(value),
                MemoryDataType.Int16 => short.Parse(value),
                MemoryDataType.UInt16 => ushort.Parse(value),
                MemoryDataType.Int32 => int.Parse(value),
                MemoryDataType.UInt32 => uint.Parse(value),
                MemoryDataType.Int64 => long.Parse(value),
                MemoryDataType.UInt64 => ulong.Parse(value),
                MemoryDataType.Float => float.Parse(value),
                MemoryDataType.Double => double.Parse(value),
                MemoryDataType.String => value,
                MemoryDataType.ByteArray => Convert.FromHexString(value),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
}
