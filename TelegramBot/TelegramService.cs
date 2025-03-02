using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static TelegramBot.UpdateHandler;

namespace TelegramBot;

public class TelegramService(AppDbContext appDbContext, ITelegramBotClient telegramBotClient)
{
    static bool IsValidJson(string input, [MaybeNullWhen(false)] out UserConfigurationDTO result)
    {
        result = null;
        try
        {
            result = JsonSerializer.Deserialize<UserConfigurationDTO>(input);
            return result is not null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> TryUpdateConfigAsync(long telegramUserId, string jsonConfig, CancellationToken cancellationToken)
    {
        if (!IsValidJson(jsonConfig, out var userConfigurationDTO)) return false;

        var result0 = await appDbContext.SaveChangesAsync(cancellationToken);
        var user = await appDbContext.UserConfigurations.FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, cancellationToken);
        if (user is null)
        {
            appDbContext.UserConfigurations.Add(new UserConfiguration(telegramUserId, userConfigurationDTO.Budget, userConfigurationDTO.MinChanceToBuy, userConfigurationDTO.MinChangeToSell, userConfigurationDTO.ExceptedProfit));
        }
        else
        {
            user.Budget = userConfigurationDTO.Budget;
            user.MinChanceToBuy = userConfigurationDTO.MinChanceToBuy;
            user.MinChangeToSell = userConfigurationDTO.MinChangeToSell;
            user.ExceptedProfit = userConfigurationDTO.ExceptedProfit;
            appDbContext.UserConfigurations.Update(user);
        }
        var result = await appDbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

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
        //await dbContext.SaveChangesAsync();
    }
}
