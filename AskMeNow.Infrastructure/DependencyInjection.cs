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

        // Database
        services.AddDbContext<KnowledgeBaseContext>(options =>
            options.UseSqlite("Data Source=knowledgebase.db"));

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

        return services;
    }
}
