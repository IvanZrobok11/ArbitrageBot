using ArbitrageBot.BackgroundServices.Base;
using BusinessLogic.Services;
using DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TelegramBot;

namespace ArbitrageBot.BackgroundServices;

public class AssetsBackgroundService(
    IServiceProvider services,
    IOptions<BackgroundServicesOption> options,
    ILogger<BaseTimeHostedHealthTrackedBackgroundService> logger) : BaseTimeHostedHealthTrackedBackgroundService(services, logger)
{
    protected override TimeSpan TimerPeriod => options.Value.AssetsBackgroundService;

    protected override async Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var sender = scope.ServiceProvider.GetRequiredService<TelegramAssetsSender>();

        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = await context.UserConfigurations.AsNoTracking().ToListAsync(cancellationToken);

        var commonExchangeService = scope.ServiceProvider.GetRequiredService<CommonExchangeService>();
        var pairs = await commonExchangeService.GetSmartAssetPairsAsync(2, 30, cancellationToken);

        foreach (var user in users)
        {
            foreach (var assetsPair in pairs)
            {
                if (100 - assetsPair.ExchangeForBuy.BidsPercentage <= user.MinChanceToBuy) continue;
                if (100 - assetsPair.ExchangeForSell.AsksPercentage <= user.MinChangeToSell) continue;

                var stats = assetsPair.GetStats(user.Budget);

                if (stats.USDTProfit < user.ExceptedProfit) continue;
                assetsPair.Stats.Add(stats);

                if (assetsPair.Symbol == "NGLUSDT")
                {
                    continue;
                }

                await sender.SendAsync(user.TelegramUserId, assetsPair, cancellationToken);
            }
        }

        Console.WriteLine("Hello from hosted service");
    }
}
