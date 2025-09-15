using AskMeNow.Core.Interfaces;
using AskMeNow.Infrastructure.Configuration;
using AskMeNow.Infrastructure.Data;
using AskMeNow.Infrastructure.Repositories;
using AskMeNow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AskMeNow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<AwsConfig>(configuration.GetSection("AwsConfig"));

        // Database - Use Infrastructure bin folder for organized location
        var infrastructureBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "AskMeNow.Infrastructure", "bin", "Debug", "net8.0");
        var dbPath = Path.Combine(infrastructureBinPath, "knowledgebase.db");
        services.AddDbContext<KnowledgeBaseContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Services
        services.AddScoped<IBedrockClientService, BedrockClientService>();
        services.AddScoped<IDocumentParserService, DocumentParserService>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentChunkingService, DocumentChunkingService>();
        services.AddScoped<IDocumentCacheService, DocumentCacheService>();
        
        // Knowledge Base Services
        services.AddSingleton<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IPersistentKnowledgeBaseService, PersistentKnowledgeBaseService>();
        services.AddSingleton<IFileWatcherService, FileWatcherService>();
        
        // Conversation and Auto-Suggest Services
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IAutoSuggestService, AutoSuggestService>();
        
        // Voice interaction services (using Whisper.NET + NAudio for STT)
        services.AddSingleton<ISpeechToTextService, WhisperSpeechToTextService>();
        services.AddSingleton<ITextToSpeechService, SimpleTextToSpeechService>();
        
        // Sentiment analysis and small talk services
        services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>();
        services.AddScoped<ISmallTalkService, SmallTalkService>();
        
        // Document preview service
        services.AddScoped<IDocumentPreviewService, DocumentPreviewService>();

        return services;
    }
}
