using System.Xml.Serialization;

namespace SaveState.Infrastructure.GameLibrary.Models;

/// <summary>
/// Root element of a Cheat Engine table XML file.
/// </summary>
[XmlRoot("CheatTable")]
public class CheatTable
{
    /// <summary>
    /// Cheat entries in the table.
    /// </summary>
    [XmlArray("CheatEntries")]
    [XmlArrayItem("CheatEntry")]
    public List<CheatEntry> CheatEntries { get; set; } = new();

    /// <summary>
    /// User-defined symbols.
    /// </summary>
    [XmlArray("UserdefinedSymbols")]
    [XmlArrayItem("Symbol")]
    public List<UserDefinedSymbol>? UserdefinedSymbols { get; set; }

    /// <summary>
    /// Structures defined in the table.
    /// </summary>
    [XmlElement("Structures")]
    public Structures? Structures { get; set; }
}

/// <summary>
/// Represents a single cheat entry in a Cheat Engine table.
/// </summary>
public class CheatEntry
{
    /// <summary>
    /// Unique identifier for the entry.
    /// </summary>
    [XmlElement("ID")]
    public int Id { get; set; }

    /// <summary>
    /// Description/name of the cheat.
    /// </summary>
    [XmlElement("Description")]
    public string? Description { get; set; }

    /// <summary>
    /// The memory address or pointer path.
    /// </summary>
    [XmlElement("Address")]
    public string? Address { get; set; }

    /// <summary>
    /// Address in Cheat Engine's internal format (alternative to Address).
    /// </summary>
    [XmlElement("AddressString")]
    public string? AddressString { get; set; }

    /// <summary>
    /// Variable type (4 Bytes, Float, Double, Byte, 2 Bytes, 8 Bytes, etc.).
    /// </summary>
    [XmlElement("VariableType")]
    public string? VariableType { get; set; }

    /// <summary>
    /// For pointer entries: the base address.
    /// </summary>
    [XmlElement("AddressOfPointer")]
    public string? AddressOfPointer { get; set; }

    /// <summary>
    /// For pointer entries: the offset chain.
    /// </summary>
    [XmlArray("Offsets")]
    [XmlArrayItem("Offset")]
    public List<string>? Offsets { get; set; }

    /// <summary>
    /// Lua script for advanced cheats.
    /// </summary>
    [XmlElement("AssemblerScript")]
    public string? AssemblerScript { get; set; }

    /// <summary>
    /// Lua script for the entry.
    /// </summary>
    [XmlElement("LuaScript")]
    public string? LuaScript { get; set; }

    /// <summary>
    /// Whether the entry is active/enabled.
    /// </summary>
    [XmlElement("Active")]
    public bool Active { get; set; }

    /// <summary>
    /// Whether the entry is shown as a checkbox.
    /// </summary>
    [XmlElement("ShowAsCheckbox")]
    public bool ShowAsCheckbox { get; set; }

    /// <summary>
    /// Color associated with the entry.
    /// </summary>
    [XmlElement("Color")]
    public string? Color { get; set; }

    /// <summary>
    /// Group header for organizing entries.
    /// </summary>
    [XmlElement("GroupHeader")]
    public bool GroupHeader { get; set; }

    /// <summary>
    /// Nested child entries.
    /// </summary>
    [XmlArray("CheatEntries")]
    [XmlArrayItem("CheatEntry")]
    public List<CheatEntry>? CheatEntries { get; set; }

    /// <summary>
    /// Last value (for display purposes).
    /// </summary>
    [XmlElement("LastValue")]
    public string? LastValue { get; set; }

    /// <summary>
    /// Whether the address is a pointer.
    /// </summary>
    public bool IsPointer => Offsets != null && Offsets.Count > 0;

    /// <summary>
    /// Whether this entry is a script entry.
    /// </summary>
    public bool IsScript => !string.IsNullOrEmpty(AssemblerScript) || !string.IsNullOrEmpty(LuaScript);

    /// <summary>
    /// Gets the display name, cleaning up Cheat Engine formatting.
    /// </summary>
    public string GetDisplayName()
    {
        if (string.IsNullOrEmpty(Description))
            return $"Entry {Id}";

        // Remove quotes that Cheat Engine wraps descriptions in
        var name = Description.Trim('"');
        
        // Remove common prefixes like "[X]" or "->"
        name = System.Text.RegularExpressions.Regex.Replace(name, @"^\[[\sxX\*]\]\s*", "");
        name = System.Text.RegularExpressions.Regex.Replace(name, @"^->\s*", "");
        
        return name;
    }
}

/// <summary>
/// User-defined symbol in Cheat Engine.
/// </summary>
public class UserDefinedSymbol
{
    /// <summary>
    /// Symbol name.
    /// </summary>
    [XmlAttribute("Name")]
    public string? Name { get; set; }

    /// <summary>
    /// Symbol address.
    /// </summary>
    [XmlAttribute("Address")]
    public string? Address { get; set; }
}

/// <summary>
/// Structures section in Cheat Engine table.
/// </summary>
public class Structures
{
    /// <summary>
    /// List of structures.
    /// </summary>
    [XmlElement("Structure")]
    public List<Structure>? StructureList { get; set; }
}

/// <summary>
/// Structure definition.
/// </summary>
public class Structure
{
    /// <summary>
    /// Structure name.
    /// </summary>
    [XmlAttribute("Name")]
    public string? Name { get; set; }

    /// <summary>
    /// Structure elements.
    /// </summary>
    [XmlElement("Element")]
    public List<StructureElement>? Elements { get; set; }
}

/// <summary>
/// Element within a structure.
/// </summary>
public class StructureElement
{
    /// <summary>
    /// Element name.
    /// </summary>
    [XmlAttribute("Name")]
    public string? Name { get; set; }

    /// <summary>
    /// Offset within the structure.
    /// </summary>
    [XmlAttribute("Offset")]
    public int Offset { get; set; }

    /// <summary>
    /// Variable type.
    /// </summary>
    [XmlAttribute("Type")]
    public string? Type { get; set; }
}

/// <summary>
/// Represents a parsed memory address from Cheat Engine format.
/// </summary>
public class ParsedAddress
{
    /// <summary>
    /// The module name (e.g., "Game.exe"). Null if absolute address.
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// The offset from the module or absolute address.
    /// </summary>
    public long Offset { get; set; }

    /// <summary>
    /// Whether this is an absolute address (no module).
    /// </summary>
    public bool IsAbsolute => string.IsNullOrEmpty(ModuleName);

    /// <summary>
    /// Whether this is a pointer path (has offsets chain).
    /// </summary>
    public bool IsPointer { get; set; }

    /// <summary>
    /// Pointer offset chain (if IsPointer is true).
    /// </summary>
    public List<long> PointerOffsets { get; set; } = new();

    /// <summary>
    /// The base address for pointers.
    /// </summary>
    public long? BaseAddress { get; set; }

    /// <summary>
    /// Raw address string from the CT file.
    /// </summary>
    public string RawAddress { get; set; } = "";

    /// <summary>
    /// Parses a Cheat Engine address string.
    /// </summary>
    public static ParsedAddress Parse(string address)
    {
        var result = new ParsedAddress { RawAddress = address };

        if (string.IsNullOrWhiteSpace(address))
            return result;

        address = address.Trim().Trim('"');

        // Check for pointer notation: "[[[Game.exe+1234]+567]+89]"
        if (address.Contains("[", StringComparison.Ordinal) && address.Contains("]", StringComparison.Ordinal))
        {
            result.IsPointer = true;
            ParsePointerAddress(result, address);
            return result;
        }

        // Check for module+offset format: "Game.exe+123456"
        var plusIndex = address.IndexOf('+');
        if (plusIndex > 0)
        {
            result.ModuleName = address[..plusIndex].Trim();
            var offsetStr = address[(plusIndex + 1)..].Trim();
            result.Offset = ParseHexOrDecimal(offsetStr);
            return result;
        }

        // Absolute address: "0x7FF123456789" or "12345678"
        result.Offset = ParseHexOrDecimal(address);
        return result;
    }

    private static void ParsePointerAddress(ParsedAddress result, string address)
    {
        // Remove outer brackets and split by ][ to get offset chain
        address = address.Trim('[', ']');
        var parts = address.Split(new[] { "+" }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Trim().Trim('[', ']');

            if (i == 0)
            {
                // First part is the base
                var plusIndex = part.IndexOf('+');
                if (plusIndex > 0)
                {
                    result.ModuleName = part[..plusIndex].Trim();
                    result.BaseAddress = ParseHexOrDecimal(part[(plusIndex + 1)..].Trim());
                }
                else
                {
                    result.BaseAddress = ParseHexOrDecimal(part);
                }
            }
            else
            {
                // Subsequent parts are offsets
                result.PointerOffsets.Add(ParseHexOrDecimal(part));
            }
        }
    }

    private static long ParseHexOrDecimal(string value)
    {
        value = value.Trim();

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return long.TryParse(value[2..], System.Globalization.NumberStyles.HexNumber, null, out var hexResult)
                ? hexResult : 0;
        }

        // Try hex without 0x prefix
        if (long.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out var result))
            return result;

        // Try decimal
        return long.TryParse(value, out var decResult) ? decResult : 0;
    }
}

/// <summary>
/// Mapping between Cheat Engine variable types and our internal types.
/// </summary>
public static class VariableTypeMappings
{
    /// <summary>
    /// Maps Cheat Engine variable types to internal value types.
    /// </summary>
    public static readonly Dictionary<string, string> CheatEngineToInternal = new(StringComparer.OrdinalIgnoreCase)
    {
        { "4 Bytes", "int32" },
        { "4 bytes", "int32" },
        { "Float", "float" },
        { "float", "float" },
        { "Double", "double" },
        { "double", "double" },
        { "Byte", "byte" },
        { "byte", "byte" },
        { "2 Bytes", "int16" },
        { "2 bytes", "int16" },
        { "8 Bytes", "int64" },
        { "8 bytes", "int64" },
        { "Integer", "int32" },
        { "String", "string" },
        { "Array of byte", "bytearray" },
        { "Array of Byte", "bytearray" },
        { "Binary", "binary" }
    };

    /// <summary>
    /// Gets the internal type for a Cheat Engine type.
    /// </summary>
    public static string? GetInternalType(string cheatEngineType)
    {
        if (string.IsNullOrWhiteSpace(cheatEngineType))
            return "int32"; // Default

        return CheatEngineToInternal.TryGetValue(cheatEngineType.Trim(), out var internalType)
            ? internalType
            : null;
    }

    /// <summary>
    /// Checks if a Cheat Engine type is supported.
    /// </summary>
    public static bool IsSupported(string cheatEngineType)
    {
        return !string.IsNullOrWhiteSpace(cheatEngineType) &&
               CheatEngineToInternal.ContainsKey(cheatEngineType.Trim());
    }

    /// <summary>
    /// Gets all supported Cheat Engine types.
    /// </summary>
    public static IEnumerable<string> GetSupportedTypes()
    {
        return CheatEngineToInternal.Keys.Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
