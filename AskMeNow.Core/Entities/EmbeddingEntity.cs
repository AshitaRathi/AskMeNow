using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskMeNow.Core.Entities;

[Table("Embeddings")]
public class EmbeddingEntity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int DocumentId { get; set; }
    
    public int ChunkIndex { get; set; }
    
    [Required]
    [MaxLength(4000)]
    public string TextChunk { get; set; } = string.Empty;
    
    [Required]
    public byte[] Vector { get; set; } = Array.Empty<byte>();
    
    public int VectorDimensions { get; set; }
    
    [MaxLength(50)]
    public string ModelVersion { get; set; } = "1.0";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    [ForeignKey("DocumentId")]
    public virtual DocumentEntity Document { get; set; } = null!;
}
