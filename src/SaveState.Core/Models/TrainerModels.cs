namespace SaveState.Core.Models;

public class TrainerDefinition
{
    public string ProcessName { get; set; } = string.Empty;
    public string GameTitle { get; set; } = string.Empty;
    public List<CheatDefinition> Cheats { get; set; } = new();
}

public class CheatDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty; // Supports pointers like "module+offset"
    public string Type { get; set; } = "int"; // int, float
    public string Value { get; set; } = "0";
    public bool IsFreeze { get; set; } // If true, keep writing the value every tick
    public bool IsActive { get; set; }
}
