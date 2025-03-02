using DAL;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot;

public class TelegramService(AppDbContext dbContext, ITelegramBotClient telegramBotClient)
{
    public async Task HandleUpdateAsync(Update update)
    {
        if (update.Type == UpdateType.Message && update.Message.Text != null)
        {
            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text;

            if (text == "/set-configuration")
            {
                await telegramBotClient.SendTextMessageAsync(chatId, "Please send configuration");
            }
            else if (text.StartsWith("config:"))
            {
                var configValue = text.Substring(7).Trim();
                await SaveConfigurationAsync(chatId, configValue);
                await telegramBotClient.SendTextMessageAsync(chatId, "Configuration saved successfully!");
            }
        }
    }

    public async Task SaveConfigurationAsync(long userId, string config)
    {
        //var existingConfig = await dbContext.UserConfigurations.FirstOrDefaultAsync(c => c.UserId == userId);

        //if (existingConfig == null)
        //{
        //    dbContext.UserConfigurations.Add(new UserConfiguration { UserId = userId, Configuration = config });
        //}
        //else
        //{
        //    existingConfig.Configuration = config;
        //}
        await dbContext.SaveChangesAsync();
    }
}
