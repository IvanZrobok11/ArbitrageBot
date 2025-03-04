using BusinessLogic.Services;
using DAL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot;

public class UpdateHandler(ITelegramBotClient bot,
    ILogger<UpdateHandler> logger,
    UserConfigurationService userConfigurationService,
    AppDbContext appDbContext,
    IOptionsSnapshot<BotConfiguration> options) : IUpdateHandler
{
    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        logger.LogInformation("HandleError: {Exception}", exception);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public bool IsValidPassword(string? phrasePassword)
    {
        if (phrasePassword is null)
        {
            return false;
        }
        return phrasePassword == options.Value.BotAuthPhrase;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await (update switch
        {
            { Message: { } message } => OnMessage(message, cancellationToken),
            { EditedMessage: { } message } => OnMessage(message, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        });
    }

    private async Task OnMessage(Message message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Receive message type: {MessageType}", message.Type);

        if (message.Text is null || message.From is null) return;
        var messageText = message.Text;

        Message sentMessage = await (messageText.Split(' ')[0] switch
        {
            "/start" or "/help" => Usage(message, cancellationToken),
            "/get_config" => GetConfig(message.From, message.Chat, message.Text, cancellationToken),
            _ => HandleMessage(message.From, message.Chat, message.Text, cancellationToken)
        });
        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.Id);
    }

    private async Task<Message> Usage(Message msg, CancellationToken cancellationToken)
    {
        const string usage = """
            <b><u>Bot menu</u></b>:
            /get_config     - get current configuration
            /help           - help
        """;
        return await bot.SendMessage(msg.Chat, usage, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<Message> GetConfig(Telegram.Bot.Types.User telegramUser, Chat chat, string? messageText, CancellationToken cancellationToken)
    {
        var user = await userConfigurationService.GetOrCreateUser(telegramUser.Id, telegramUser.Username, cancellationToken);

        if (user.AuthPhrase is null || !IsValidPassword(user.AuthPhrase))
        {
            return await bot.SendMessage(chat, "Please set a correct password", cancellationToken: cancellationToken);
        }

        var jsonConfig = await userConfigurationService.GetUserStringConfig(telegramUser.Id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(jsonConfig))
        {
            return await bot.SendMessage(chat, jsonConfig, cancellationToken: cancellationToken);
        }

        var json = """
        {
          "Budget": 10000,
          "MinChanceToBuy": 50,
          "MinChangeToSell": 50,
          "ExceptedProfit": 5
        }
        """;
        return await bot.SendMessage(chat, $"Error: cannot get config, send config in format:\n\r {json}", cancellationToken: cancellationToken);
    }

    private async Task<Message> HandleMessage(Telegram.Bot.Types.User telegramUser, Chat chat, string? messageText, CancellationToken cancellationToken)
    {
        var userId = telegramUser.Id;
        var input = messageText?.Trim();

        var user = await userConfigurationService.GetOrCreateUser(userId, telegramUser.Username, cancellationToken);
        if (user.AuthPhrase is null || !IsValidPassword(user.AuthPhrase))
        {
            if (input is null || !IsValidPassword(input))
            {
                return await bot.SendMessage(chat, "Please set a correct password", cancellationToken: cancellationToken);
            }

            user.AuthPhrase = input;
            await appDbContext.SaveChangesAsync(cancellationToken);

            return await bot.SendMessage(chat, "You are logged in, please set a config", cancellationToken: cancellationToken);
        }

        var successUpdate = await userConfigurationService.TryUpdateConfigAsync(userId, input, cancellationToken);
        if (successUpdate)
        {
            return await bot.SendMessage(chat, "Config was saved", cancellationToken: cancellationToken);
        }
        return await GetConfig(telegramUser, chat, messageText, cancellationToken);
    }

    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}
