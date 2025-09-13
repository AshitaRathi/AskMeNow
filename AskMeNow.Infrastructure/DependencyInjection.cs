using AskMeNow.Core.Interfaces;
using AskMeNow.Infrastructure.Configuration;
using AskMeNow.Infrastructure.Repositories;
using AskMeNow.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AskMeNow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<AwsConfig>(configuration.GetSection("AwsConfig"));

        // Services
        services.AddScoped<IBedrockClientService, BedrockClientService>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentChunkingService, DocumentChunkingService>();
        services.AddScoped<IDocumentCacheService, DocumentCacheService>();

        return services;
    }
}
