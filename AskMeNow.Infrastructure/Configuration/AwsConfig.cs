namespace AskMeNow.Infrastructure.Configuration;

public class AwsConfig
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string ModelId { get; set; } = "anthropic.claude-3-sonnet-20240229-v1:0";
}
