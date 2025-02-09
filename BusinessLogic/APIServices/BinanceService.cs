using BusinessLogic.Interfaces;
using BusinessLogic.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace BusinessLogic.APIServices;

public class BinanceService : ICryptoExchangeApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public BinanceService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiUrl = configuration["CryptoApi:BinanceUrl"];
    }

    //public async Task<List<CryptoPrice>?> GetPricesAsync(CancellationToken cancellationToken)
    //{
    //    string response = await _httpClient.GetStringAsync(_apiUrl, cancellationToken);
    //    var prices = JsonConvert.DeserializeObject<List<BinancePrice>>(response);
    //    return prices.Select(x => new CryptoPrice("Binance", x.Symbol, x.Price)).ToList();
    //}
    public async Task<List<CryptoPrice>?> GetPricesAsync(CancellationToken cancellationToken)
    {
        // 1. Отримуємо всі доступні торгові пари
        var exchangeInfoUrl = "https://api.binance.com/api/v3/exchangeInfo";
        var exchangeInfoResponse = await _httpClient.GetStringAsync(exchangeInfoUrl);
        using var exchangeJson = JsonDocument.Parse(exchangeInfoResponse);

        var spotPairs = new HashSet<string>();

        foreach (var symbol in exchangeJson.RootElement.GetProperty("symbols").EnumerateArray())
        {
            if (symbol.GetProperty("status").GetString() == "TRADING")
            {
                spotPairs.Add(symbol.GetProperty("symbol").GetString());
            }
        }

        // 2. Отримуємо всі ціни
        var pricesUrl = "https://api.binance.com/api/v3/ticker/price";
        var pricesResponse = await _httpClient.GetStringAsync(pricesUrl);
        using var pricesJson = JsonDocument.Parse(pricesResponse);

        var spotPrices = new List<CryptoPrice>();

        foreach (var price in pricesJson.RootElement.EnumerateArray())
        {
            string symbol = price.GetProperty("symbol").GetString();
            var strPrice = price.GetProperty("price").GetString();
            decimal lastPrice = Convert.ToDecimal(strPrice, System.Globalization.CultureInfo.InvariantCulture);

            if (spotPairs.Contains(symbol))  // Фільтруємо тільки ті, що є у Spot
            {
                spotPrices.Add(new CryptoPrice(ExchangeType.Binance, symbol, lastPrice));
            }
        }

        //if (spotPrices.Any(x => x.Symbol == "WAVESUSDT"))
        //{

        //}
        return spotPrices;
    }
}

public record BinancePrice(string ExchangeName, string Symbol, double Price);