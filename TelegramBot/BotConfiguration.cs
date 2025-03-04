namespace TelegramBot;

public class BotConfiguration
{
    public required string BotToken { get; init; }
    public required Uri BotWebhookUrl { get; init; }
    public required string SecretToken { get; init; }
    public required string BotAuthPhrase { get; set; }
}
