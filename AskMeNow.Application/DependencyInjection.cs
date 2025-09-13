using AskMeNow.Application.Handlers;
using AskMeNow.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AskMeNow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IFAQService, FAQService>();
        services.AddScoped<IQuestionHandler, QuestionHandler>();

        return services;
    }
}
