namespace SaveState.Application.Mugen.Models.DreamLogic;

/// <summary>
/// Memory palace data.
/// </summary>
public class MemoryPalace
{
    public string PalaceId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public string ArenaId { get; set; } = default!;
    public IReadOnlyList<MemoryRoom> Rooms { get; set; } = default!;
    public PalaceLayout Layout { get; set; } = default!;
    public DateTime ConstructedAt { get; set; } = default!;
}

/// <summary>
/// Memory room data.
/// </summary>
public class MemoryRoom
{
    public string RoomId { get; set; } = default!;
    public string Memory { get; set; } = default!;
    public System.Numerics.Vector3 Position { get; set; } = default!;
    public string AssociatedEmotion { get; set; } = default!;
    public RoomType RoomType { get; set; } = default!;
}

/// <summary>
/// Memory palace creation request.
/// </summary>
public class MemoryPalaceRequest
{
    public string PlayerId { get; set; } = default!;
    public string ArenaId { get; set; } = default!;
    public IReadOnlyList<string> Memories { get; set; } = default!;
}
