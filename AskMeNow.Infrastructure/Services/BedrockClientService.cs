using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using AskMeNow.Core.Interfaces;
using AskMeNow.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AskMeNow.Infrastructure.Services
{
    public class BedrockClientService : IBedrockClientService
    {
        private readonly AmazonBedrockRuntimeClient _bedrockClient;
        private readonly AwsConfig _awsConfig;

        public BedrockClientService(IOptions<AwsConfig> awsConfig)
        {
            _awsConfig = awsConfig.Value;

            var config = new AmazonBedrockRuntimeConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_awsConfig.Region)
            };

            _bedrockClient = new AmazonBedrockRuntimeClient(_awsConfig.AccessKey, _awsConfig.SecretKey, config);
        }

        public async Task<string> GenerateAnswerAsync(string question, string context)
        {
            try
            {
                // Truncate context if it's too long to avoid token limits
                var maxContextLength = 8000; // Leave room for question and prompt
                var truncatedContext = context.Length > maxContextLength
                    ? context.Substring(0, maxContextLength) + "..."
                    : context;

                var prompt = $@"You are a helpful AI assistant that answers questions based on the provided context. 
Please provide a clear, accurate, and helpful answer based on the information given.
If the context doesn't contain enough information to answer the question, please say so.

Context:
{truncatedContext}

Question: {question}

Answer:";

                var requestBody = new
                {
                    anthropic_version = "bedrock-2023-05-31",
                    max_tokens = 1500,
                    temperature = 0.1,
                    messages = new[]
                    {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                }
                };

                var requestBodyJson = JsonSerializer.Serialize(requestBody);
                var request = new InvokeModelRequest
                {
                    ModelId = _awsConfig.ModelId,
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson)),
                    ContentType = "application/json"
                };

                var response = await _bedrockClient.InvokeModelAsync(request);

                using var reader = new StreamReader(response.Body);
                var responseJson = await reader.ReadToEndAsync();

                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseJson);

                if (responseObject.TryGetProperty("content", out var content) &&
                    content.GetArrayLength() > 0)
                {
                    var firstContent = content[0];
                    if (firstContent.TryGetProperty("text", out var text))
                    {
                        var answer = text.GetString() ?? "No answer generated.";
                        return answer.Trim();
                    }
                }

                return "I'm sorry, I wasn't able to generate a response. Please try rephrasing your question.";
            }
            catch (Amazon.BedrockRuntime.Model.ValidationException ex)
            {
                return $"Invalid request: {ex.Message}";
            }
            catch (Amazon.BedrockRuntime.Model.ThrottlingException)
            {
                return "The service is currently busy. Please wait a moment and try again.";
            }
            catch (Amazon.BedrockRuntime.Model.AccessDeniedException)
            {
                return "Access denied. Please check your AWS credentials and permissions.";
            }
            catch (Exception ex)
            {
                return $"An error occurred while processing your question: {ex.Message}";
            }
        }

        public async Task<string> GenerateSuggestionsAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    anthropic_version = "bedrock-2023-05-31",
                    max_tokens = 500,
                    temperature = 0.3,
                    messages = new[]
                    {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                }
                };

                var requestBodyJson = JsonSerializer.Serialize(requestBody);
                var request = new InvokeModelRequest
                {
                    ModelId = _awsConfig.ModelId,
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson)),
                    ContentType = "application/json"
                };

                var response = await _bedrockClient.InvokeModelAsync(request);

                using var reader = new StreamReader(response.Body);
                var responseJson = await reader.ReadToEndAsync();

                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseJson);

                if (responseObject.TryGetProperty("content", out var content) &&
                    content.GetArrayLength() > 0)
                {
                    var firstContent = content[0];
                    if (firstContent.TryGetProperty("text", out var text))
                    {
                        var suggestions = text.GetString() ?? "[]";
                        return suggestions.Trim();
                    }
                }

                return "[]";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating suggestions: {ex.Message}");
                return "[]";
            }
        }
    }
}