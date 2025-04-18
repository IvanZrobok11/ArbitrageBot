using BusinessLogic.Services;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
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

        if (!await IsAuthorized(message.From.Id, cancellationToken))
        {
            Message sentMessage = await (messageText.Split(' ')[0] switch
            {
                "/start" or "/help" => Usage(message, cancellationToken),
                _ => HandleMessage(message, cancellationToken)
            });

            logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.Id);
            return;
        }
        else
        {
            Message sentMessage = await (messageText.Split(' ')[0] switch
            {
                "/start" or "/help" => Usage(message, cancellationToken),
                "/get_config" => GetConfig(message.From, message.Chat, message.Text, cancellationToken),
                "/get_black_list" => GetBlackList(message.Chat, cancellationToken),
                "/to_black_list" => SetBlackAsset(message.Chat, messageText, cancellationToken),
                "/from_black_list" => RemoveFromBlackList(message.Chat, messageText, cancellationToken),
                _ => HandleMessage(message, cancellationToken)
            });
            logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.Id);
        }
    }

    private async Task<Message> Usage(Message msg, CancellationToken cancellationToken)
    {
        const string usage = """
            <b><u>Bot menu</u></b>:
            /help            - help
            /get_config      - get current configuration
            /to_black_list   - set asset you don't want to be sent
            /get_black_list  - return full black list
            /from_black_list - remove asset from black list after command
        """;
        return await bot.SendMessage(msg.Chat, usage, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<bool> IsAuthorized(long telegramUserId, CancellationToken cancellationToken)
    {
        var user = await appDbContext.Users
            .FirstOrDefaultAsync(c => c.TelegramUserId == telegramUserId, cancellationToken);
        return user?.AuthPhrase is not null && IsValidPassword(user.AuthPhrase);
    }

    private async Task<Message> GetConfig(Telegram.Bot.Types.User telegramUser, Chat chat, string? messageText, CancellationToken cancellationToken)
    {
        var jsonConfig = await userConfigurationService.GetUserStringConfig(telegramUser.Id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(jsonConfig))
        {
            return await bot.SendMessage(chat, $"<code>{jsonConfig}</code>", ParseMode.Html, cancellationToken: cancellationToken);
        }

        var json = """
        {
          "Budget": 10000,
          "MinChanceToBuy": 50,
          "MinChangeToSell": 50,
          "ExceptedProfit": 5,
          "TicketFilter":"USDT"
        }
        """;
        return await bot.SendMessage(chat, $"Error: cannot get config, send config in format:\n\r {json}", cancellationToken: cancellationToken);
    }

    private async Task<Message> HandleMessage(Message message, CancellationToken cancellationToken)
    {
        var telegramUser = message.From ?? throw new Exception("From is null");
        var userId = telegramUser.Id;
        var chat = message.Chat;
        var input = message.Text?.Trim();

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
        return await Usage(message, cancellationToken);
    }

    private async Task<Message> SetBlackAsset(Chat chat, string? messageText, CancellationToken cancellationToken)
    {
        var assetName = messageText?.Split(' ').ElementAtOrDefault(1)?.ToUpper();
        if (string.IsNullOrWhiteSpace(assetName))
        {
            return await bot.SendMessage(chat, "Invalid asset command format, try <code>/to_black_list BTCUSDT</code>", ParseMode.Html, cancellationToken: cancellationToken);
        }

        appDbContext.BlackAssets.Add(new BlackAsset(assetName));
        await appDbContext.SaveChangesAsync(cancellationToken);

        return await bot.SendMessage(chat, "Saved to black list", cancellationToken: cancellationToken);
    }

    private async Task<Message> GetBlackList(Chat chat, CancellationToken cancellationToken)
    {
        var assets = await appDbContext.BlackAssets.AsNoTracking().ToListAsync(cancellationToken);
        var builder = new StringBuilder();
        foreach (var asset in assets)
        {
            builder.AppendLine(asset.Name);
        }
        var text = builder.ToString();
        return await bot.SendMessage(chat, string.IsNullOrEmpty(text) ? "List is empty" : text, cancellationToken: cancellationToken);
    }

    private async Task<Message> RemoveFromBlackList(Chat chat, string? messageText, CancellationToken cancellationToken)
    {
        var assetName = messageText?.Split(' ').ElementAtOrDefault(1)?.ToUpper();

        if (string.IsNullOrWhiteSpace(assetName))
        {
            return await bot.SendMessage(chat, "Invalid asset command format, try <code>/from_black_list BTCUSDT</code>", ParseMode.Html, cancellationToken: cancellationToken);
        }

        var assets = await appDbContext.BlackAssets.Where(x => x.Name == assetName).ToListAsync(cancellationToken);
        appDbContext.RemoveRange(assets);
        await appDbContext.SaveChangesAsync(cancellationToken);

        return await bot.SendMessage(chat, "Removed from black list", cancellationToken: cancellationToken);
    }

    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}
