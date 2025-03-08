using BusinessLogic.Extensions;
using BusinessLogic.Models;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace TelegramBot;

public class TelegramAssetsSender(ITelegramBotClient TelegramBotClient)
{
    public async Task SendAsync(long telegramUserId, AssetsPairViewModel assetsPair, CancellationToken cancellationToken)
    {
        try
        {
            // Convert JSON to HTML-formatted message
            string htmlMessage = ConvertToFormattedHtml(assetsPair);

            // Send message with HTML parsing
            await TelegramBotClient.SendMessage(
                chatId: telegramUserId,
                text: htmlMessage,
                parseMode: ParseMode.Html
            );

            Console.WriteLine("HTML message sent successfully!");
        }
        catch (Exception ex)
        {
            var json = JsonSerializer.Serialize(assetsPair, new JsonSerializerOptions { WriteIndented = true });
            await TelegramBotClient.SendMessage(telegramUserId, json, cancellationToken: cancellationToken);
            Console.WriteLine($"Error sending HTML message: {ex.Message}");
        }
    }

    private string ConvertToFormattedHtml(AssetsPairViewModel model)
    {
        var htmlBuilder = new StringBuilder();

        // Symbol and Basic Info
        htmlBuilder.AppendLine($"<b>📊 Trading Symbol: {model.Symbol}</b>");
        htmlBuilder.AppendLine($"Price Difference: {model.DiffPercent.RoundDecimals(2)}%\n");

        // Buy Exchange Details
        var buyExchange = model.ExchangeForBuy;
        if (buyExchange != null)
        {
            htmlBuilder.AppendLine("<b>🟢 Buy Exchange Details:</b>");
            htmlBuilder.AppendLine($"Exchange: {buyExchange.Type}");
            htmlBuilder.AppendLine($"Network: {buyExchange.Network.Name}");
            htmlBuilder.AppendLine($"Price: {buyExchange.Price.RoundDecimals(6)}");
            htmlBuilder.AppendLine($"Asks: {buyExchange.AsksPercentage.RoundDecimals(1)}%");
            htmlBuilder.AppendLine($"Bids: {buyExchange.BidsPercentage.RoundDecimals(1)}%");
            htmlBuilder.AppendLine($"Liquidity: {buyExchange.LiquidityPercentage.RoundDecimals(1)}%\n");
        }

        // Sell Exchange Details
        var sellExchange = model.ExchangeForSell;
        if (sellExchange != null)
        {
            htmlBuilder.AppendLine("<b>🔴 Sell Exchange Details:</b>");
            htmlBuilder.AppendLine($"Exchange: {sellExchange.Type}");
            htmlBuilder.AppendLine($"Network: {sellExchange.Network.Name}");
            htmlBuilder.AppendLine($"Price: {sellExchange.Price.RoundDecimals(9)}");
            htmlBuilder.AppendLine($"Asks: {sellExchange.AsksPercentage.RoundDecimals(1)}%");
            htmlBuilder.AppendLine($"Bids: {sellExchange.BidsPercentage.RoundDecimals(1)}%");
            htmlBuilder.AppendLine($"Liquidity: {sellExchange.LiquidityPercentage.RoundDecimals(1)}%\n");
        }

        // Profit Statistics
        var stats = model.Stats;
        if (stats != null && stats.Count > 0)
        {
            htmlBuilder.AppendLine("<b>📈 Profit Statistics:</b>");
            foreach (var stat in stats.OrderBy(x => x.USDTBudget))
            {
                htmlBuilder.AppendLine(
                    $"💲 Budget: {stat.USDTBudget.RoundDecimals(3)} USDT | " +
                    $"Profit: {stat.USDTProfit.RoundDecimals(2)} USDT"
                );
                htmlBuilder.AppendLine($"🏦 Fees: {stat.Fees} USDT");
            }
        }

        return htmlBuilder.ToString();
    }
}