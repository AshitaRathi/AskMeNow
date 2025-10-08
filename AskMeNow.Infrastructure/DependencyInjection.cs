using AskMeNow.Core.Interfaces;
using AskMeNow.Infrastructure.Configuration;
using AskMeNow.Infrastructure.Data;
using AskMeNow.Infrastructure.Repositories;
using AskMeNow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AskMeNow.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AwsConfig>(configuration.GetSection("AwsConfig"));

            var infrastructureBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "AskMeNow.Infrastructure", "bin", "Debug", "net8.0");
            var dbPath = Path.Combine(infrastructureBinPath, "knowledgebase.db");
            services.AddDbContext<KnowledgeBaseContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            services.AddScoped<IBedrockClientService, BedrockClientService>();
            services.AddScoped<IDocumentParserService, DocumentParserService>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<IDocumentChunkingService, DocumentChunkingService>();
            services.AddScoped<IDocumentCacheService, DocumentCacheService>();

            services.AddScoped<ISemanticChunkingService, SemanticChunkingService>();
            services.AddScoped<IEnhancedRetrievalService, EnhancedRetrievalService>();
            services.AddScoped<IEnhancedContextService, EnhancedContextService>();

            services.AddSingleton<IEmbeddingService, EmbeddingService>();
            services.AddScoped<IPersistentKnowledgeBaseService, PersistentKnowledgeBaseService>();
            services.AddSingleton<IFileWatcherService, FileWatcherService>();

            services.AddScoped<IConversationService, ConversationService>();
            services.AddScoped<IAutoSuggestService, AutoSuggestService>();

            services.AddSingleton<ISpeechToTextService, WhisperSpeechToTextService>();
            services.AddSingleton<ITextToSpeechService, SimpleTextToSpeechService>();

            services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>();
            services.AddScoped<ISmallTalkService, SmallTalkService>();

            services.AddScoped<IDocumentPreviewService, DocumentPreviewService>();

            return services;
        }
    }
}