using BusinessLogic.Interfaces;
using BusinessLogic.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BusinessLogic.APIServices;

public class BybitService : ICryptoExchangeApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public BybitService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiUrl = configuration["CryptoApi:BybitUrl"]; // Наприклад, "https://api.bybit.com/v5/market/tickers"
    }

    //public async Task<List<CryptoPrice>?> GetPricesAsync(CancellationToken cancellationToken)
    //{
    //    string response = await _httpClient.GetStringAsync(_apiUrl);
    //    var bybitResponse = JsonConvert.DeserializeObject<BybitResponse>(response);

    //    List<CryptoPrice> prices = new();
    //    foreach (var item in bybitResponse.Result.List)
    //    {
    //        prices.Add(new CryptoPrice("ByBit", item.Symbol, item.LastPrice));
    //    }

    //    return prices;
    //}

    public async Task<List<CryptoPrice>?> GetPricesAsync(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();

        // 1. Отримуємо всі символи через v5 API
        var symbolsResponse = await httpClient.GetStringAsync("https://api.bybit.com/v5/market/instruments-info?category=spot");
        var symbolsData = JsonConvert.DeserializeObject<dynamic>(symbolsResponse);

        var activeSymbols = new HashSet<string>();

        foreach (var symbol in symbolsData.result.list)
        {
            if (symbol.status == "Trading")  // Фільтруємо тільки ті пари, які в статусі TRADING
            {
                activeSymbols.Add(symbol.symbol.ToString());
            }
        }

        // 2. Отримуємо останні ціни через v5 API

        string response = await _httpClient.GetStringAsync(_apiUrl);
        var bybitResponse = JsonConvert.DeserializeObject<BybitResponse>(response);

        var tradingPrices = new List<CryptoPrice>();

        foreach (var ticker in bybitResponse.Result.List)
        {
            string symbol = ticker.Symbol;
            var lastPrice = ticker.LastPrice;
            //decimal lastPrice = Convert.ToDecimal(strPrice, System.Globalization.CultureInfo.InvariantCulture);

            if (activeSymbols.Contains(symbol))  // Додаємо ціни тільки для активних символів
            {
                tradingPrices.Add(new CryptoPrice(ExchangeType.ByBit, symbol, lastPrice));
            }
        }

        return tradingPrices;
    }
}
