using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskMeNow.Core.Entities;

[Table("Conversations")]
public class Conversation
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string ConversationId { get; set; } = Guid.NewGuid().ToString();
    
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation property
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
