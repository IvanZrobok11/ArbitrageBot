using BusinessLogic.Services;
using DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Telegram.Bot;

namespace ArbitrageBot.BackgroundServices;

public class AssetsBackgroundService(
    IServiceProvider services,
    IOptions<BackgroundServicesOption> options,
    ILogger<BaseBackgroundService> logger) : BaseBackgroundService(services, logger)
{
    protected override TimeSpan TimerPeriod => options.Value.AssetsBackgroundService;
    //protected override TimeSpan TimerPeriod => TimeSpan.FromSeconds(30);

    protected override async Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var telegramBotClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = await context.UserConfigurations.AsNoTracking().ToListAsync(cancellationToken);

        var commonExchangeService = scope.ServiceProvider.GetRequiredService<CommonExchangeService>();
        var pairs = await commonExchangeService.GetSmartAssetPairsAsync(1, 30, true, cancellationToken);

        foreach (var user in users)
        {
            foreach (var assetsPair in pairs)
            {
                if (assetsPair.ExchangeForBuy.BidsPercentage > user.MinChanceToBuy) continue;
                if (assetsPair.ExchangeForBuy.AsksPercentage > user.MinChangeToSell) continue;

                var stats = assetsPair.GetStats(user.Budget);
                if (stats.USDTProfit < user.ExceptedProfit) continue;

                var json = JsonSerializer.Serialize(assetsPair, new JsonSerializerOptions { WriteIndented = true });
                await telegramBotClient.SendMessage(user.TelegramUserId, json, cancellationToken: cancellationToken);
            }
        }

        Console.WriteLine("Hello from hosted service2");
    }
}
