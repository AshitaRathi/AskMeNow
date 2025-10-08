using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskMeNow.Core.Entities
{
    [Table("ChatMessages")]
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Sender { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? Question { get; set; }

        public string? Answer { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int TurnNumber { get; set; }

        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; } = null!;
    }
}