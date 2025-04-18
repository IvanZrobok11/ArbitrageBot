using BusinessLogic.Models;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace BusinessLogic.Services;

public class UserConfigurationService(AppDbContext appDbContext)
{
    public async Task<User> GetOrCreateUser(long id, string? name, CancellationToken cancellationToken)
    {
        var user = await appDbContext.Users.FirstOrDefaultAsync(u => u.TelegramUserId == id, cancellationToken);
        if (user is not null)
        {
            return user;
        }
        var entryUser = appDbContext.Add(new User(id, name));
        await appDbContext.SaveChangesAsync(cancellationToken);
        return entryUser.Entity;
    }

    public async Task<string?> GetUserStringConfig(long telegramUserId, CancellationToken cancellationToken)
    {
        var user = await appDbContext.UserConfigurations.FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, cancellationToken);
        if (user is null)
        {
            return null;
        }
        var dto = new UserConfigurationDTO(user.Budget, user.MinChanceToBuy, user.MinChangeToSell, user.ExceptedProfit, user.TicketFilter);
        return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<bool> TryUpdateConfigAsync(long telegramUserId, string? jsonConfig, CancellationToken cancellationToken)
    {
        if (!IsValidJson(jsonConfig, out var userConfigurationDTO)) return false;

        var user = await appDbContext.Users
            .Include(x => x.UserConfiguration)
            .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, cancellationToken);

        if (user is null) return false;

        if (user.UserConfiguration is null)
        {
            appDbContext.UserConfigurations.Add(
                new UserConfiguration(telegramUserId,
                userConfigurationDTO.Budget,
                userConfigurationDTO.MinChanceToBuy,
                userConfigurationDTO.MinChangeToSell,
                userConfigurationDTO.ExceptedProfit,
                userConfigurationDTO.TickerFilter));
        }
        else
        {
            user.UserConfiguration.Budget = userConfigurationDTO.Budget;
            user.UserConfiguration.MinChanceToBuy = userConfigurationDTO.MinChanceToBuy;
            user.UserConfiguration.MinChangeToSell = userConfigurationDTO.MinChangeToSell;
            user.UserConfiguration.ExceptedProfit = userConfigurationDTO.ExceptedProfit;
            user.UserConfiguration.TicketFilter = userConfigurationDTO.TickerFilter;
        }
        var changesCount = await appDbContext.SaveChangesAsync(cancellationToken);
        return changesCount > 0;
    }

    private static bool IsValidJson(string? input, [MaybeNullWhen(false)] out UserConfigurationDTO result)
    {
        result = null;
        if (input is null)
        {
            return false;
        }

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
}
