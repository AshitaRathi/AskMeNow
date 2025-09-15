using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AskMeNow.Infrastructure.Data;

public class KnowledgeBaseContextFactory : IDesignTimeDbContextFactory<KnowledgeBaseContext>
{
    public KnowledgeBaseContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KnowledgeBaseContext>();
        // Use Infrastructure bin folder for organized location
        var infrastructureBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "AskMeNow.Infrastructure", "bin", "Debug", "net8.0");
        var dbPath = Path.Combine(infrastructureBinPath, "knowledgebase.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new KnowledgeBaseContext(optionsBuilder.Options);
    }
}
