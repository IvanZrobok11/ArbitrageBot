namespace BusinessLogic.Models;

public class HttpClientPolicy
{
    public const string SectionKey = "HttpClient.Policy";
    public required int HandlerLifetime { get; set; }
    public required int RetryCount { get; set; }
    public required int RetryTimeout { get; set; }
    public required int Timeout { get; set; }
}

