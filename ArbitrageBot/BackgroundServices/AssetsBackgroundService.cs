using BusinessLogic.Models;
using BusinessLogic.Services;
using DAL;
using DAL.Models;
using HostedService.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Telegram.Bot;
using TelegramBot;

namespace ArbitrageBot.BackgroundServices;

public class AssetsBackgroundService(
    IServiceProvider services,
    IOptions<BackgroundServicesOption> options,
    ILogger<AssetsBackgroundService> logger) : TimePeriodicHostedService(services, logger)
{
    protected override TimeSpan TimerPeriod => options.Value.AssetsBackgroundService;

    protected override async Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var telegramBotClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userConfigurations = await context.UserConfigurations.AsNoTracking().ToListAsync(cancellationToken);
        var blackAssets = await context.BlackAssets.AsNoTracking().Select(a => a.Name).ToListAsync(cancellationToken);

        var commonExchangeService = scope.ServiceProvider.GetRequiredService<CommonExchangeService>();
        var pairs = await commonExchangeService.GetSmartAssetPairsAsync(1.5m, 30, cancellationToken);

        foreach (var user in userConfigurations)
        {
            foreach (var assetsPair in pairs)
            {
                if (blackAssets.Contains(assetsPair.Symbol)) continue;

                if (!IsTradable(assetsPair, InitDTO(user))) continue;

                if (!assetsPair.Stats.Any(x => x.Budget == user.Budget)) assetsPair.Stats.Add(assetsPair.GetStats(user.Budget));

                logger.LogInformation(JsonSerializer.Serialize(assetsPair, new JsonSerializerOptions { WriteIndented = true }));
                await telegramBotClient.SendAsync(user.TelegramUserId, assetsPair, cancellationToken);
            }
        }

        logger.LogInformation("Hello from hosted service");
        logger.LogInformation("ProcessorCount" + Environment.ProcessorCount);
    }

    public bool IsTradable(AssetsPairViewModel assetsPair, UserConfigurationDTO userConfiguration)
    {
        if (!string.IsNullOrWhiteSpace(userConfiguration.TickerFilter) && !assetsPair.Symbol.Contains(userConfiguration.TickerFilter)) return false;

        if (100 - assetsPair.ExchangeForBuy.WantToBuyPercentage <= userConfiguration.MinChanceToBuy) return false;
        if (100 - assetsPair.ExchangeForSell.WantToSellPercentage <= userConfiguration.MinChangeToSell) return false;

        var budgetStats = assetsPair.GetStats(userConfiguration.Budget);
        if (budgetStats.MaxBuyWithdrawPrice < userConfiguration.Budget || budgetStats.MaxSellWithdrawPrice < userConfiguration.Budget) return false;

        return budgetStats.Profit >= userConfiguration.ExceptedProfit;
    }

    public UserConfigurationDTO InitDTO(UserConfiguration userConfiguration)
    {
        return new UserConfigurationDTO(userConfiguration.Budget, userConfiguration.MinChanceToBuy, userConfiguration.MinChangeToSell, userConfiguration.ExceptedProfit, userConfiguration.TicketFilter);
    }
}
