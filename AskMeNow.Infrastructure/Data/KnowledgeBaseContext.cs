using AskMeNow.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AskMeNow.Infrastructure.Data;

public class KnowledgeBaseContext : DbContext
{
    public KnowledgeBaseContext(DbContextOptions<KnowledgeBaseContext> options) : base(options)
    {
    }

    public DbSet<DocumentEntity> Documents { get; set; }
    public DbSet<EmbeddingEntity> Embeddings { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DocumentEntity
        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FilePath).IsUnique();
            entity.HasIndex(e => e.LastModified);
            entity.HasIndex(e => e.FileType);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Language).HasMaxLength(10).HasDefaultValue("en");
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(10);
            entity.Property(e => e.FileSizeBytes).HasDefaultValue(0);
        });

        // Configure EmbeddingEntity
        modelBuilder.Entity<EmbeddingEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex });
            entity.Property(e => e.TextChunk).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Vector).IsRequired();
            entity.Property(e => e.ModelVersion).HasMaxLength(50).HasDefaultValue("1.0");
            
            // Configure relationship
            entity.HasOne(e => e.Document)
                  .WithMany(d => d.Embeddings)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Conversation
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConversationId).IsUnique();
            entity.HasIndex(e => e.LastActivityAt);
            entity.Property(e => e.ConversationId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(255);
        });

        // Configure ChatMessage
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ConversationId, e.TurnNumber });
            entity.Property(e => e.Sender).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Content).IsRequired();
            
            // Configure relationship
            entity.HasOne(e => e.Conversation)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
