namespace SaveState.Core.Entities;

public class RomFolder
{
    public Guid Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string PlatformName { get; set; } = string.Empty;
    public bool ScanRecursively { get; set; } = true;
    public DateTime LastScanned { get; set; }
    public int RomCount { get; set; }
}
