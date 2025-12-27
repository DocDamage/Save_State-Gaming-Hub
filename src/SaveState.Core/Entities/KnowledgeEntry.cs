using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Entities;

/// <summary>
/// Knowledge entry for RAG retrieval
/// </summary>
public class KnowledgeEntry
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The text content of this knowledge entry
    /// </summary>
    [Required]
    public string Content { get; set; } = "";

    /// <summary>
    /// Category for filtering (game_tips, cheat_guides, user_notes, system_docs)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "";

    /// <summary>
    /// Serialized embedding vector (768 floats as bytes)
    /// </summary>
    public byte[] Embedding { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Optional JSON metadata (source, game name, etc.)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When this entry was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this entry was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
