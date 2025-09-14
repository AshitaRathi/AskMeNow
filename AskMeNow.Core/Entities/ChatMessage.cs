using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskMeNow.Core.Entities;

[Table("ChatMessages")]
public class ChatMessage
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ConversationId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Sender { get; set; } = string.Empty; // "User" or "AI"
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public string? Question { get; set; } // For AI messages, store the original question
    
    public string? Answer { get; set; } // For AI messages, store the answer
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public int TurnNumber { get; set; } // Sequential turn number in conversation
    
    // Navigation property
    [ForeignKey("ConversationId")]
    public virtual Conversation Conversation { get; set; } = null!;
}
