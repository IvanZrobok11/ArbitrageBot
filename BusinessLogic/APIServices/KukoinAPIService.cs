using BusinessLogic.Interfaces;
using BusinessLogic.Models;
using System.Text.Json;

namespace BusinessLogic.APIServices
{
    public class KukoinAPIService(HttpClient httpClient) : ICryptoExchangeApiService
    {
        public async Task<List<CryptoPrice>?> GetPricesAsync(CancellationToken cancellationToken)
        {
            var response = await httpClient.GetStringAsync("https://api.kucoin.com/api/v1/symbols");

            using var doc = JsonDocument.Parse(response);
            var symbols = doc.RootElement.GetProperty("data")
                .EnumerateArray()
                .Where(s => s.GetProperty("enableTrading").GetBoolean())
                .Select(s => new { Symbol = s.GetProperty("symbol").GetString(), Market = s.GetProperty("market").GetString() });

            var response2 = await httpClient.GetStringAsync("https://api.kucoin.com/api/v1/market/allTickers");

            using var doc2 = JsonDocument.Parse(response2);
            var tickers = doc2.RootElement.GetProperty("data").GetProperty("ticker")
                .EnumerateArray()
                .Select(t => new
                {
                    Symbol = t.GetProperty("symbol").GetString(),
                    Price = t.GetProperty("last").GetString()
                });

            var result = new List<CryptoPrice>();
            foreach (var ticker in tickers)
            {
                if (symbols.Any(x => x.Symbol == ticker.Symbol))
                {
                    var price = Convert.ToDecimal(ticker.Price, System.Globalization.CultureInfo.InvariantCulture);
                    result.Add(new CryptoPrice(ExchangeType.KuCoin, ticker.Symbol.Replace("-", string.Empty), price));
                }
            }
            return result;
        }
    }
}
