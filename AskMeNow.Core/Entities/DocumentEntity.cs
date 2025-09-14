using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskMeNow.Core.Entities;

[Table("Documents")]
public class DocumentEntity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    public DateTime LastModified { get; set; }
    
    [MaxLength(10)]
    public string Language { get; set; } = "en";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public virtual ICollection<EmbeddingEntity> Embeddings { get; set; } = new List<EmbeddingEntity>();
}
