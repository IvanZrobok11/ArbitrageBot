using BusinessLogic.Extensions;
using BusinessLogic.Interfaces;
using BusinessLogic.Models;
using CryptoExchange.Net.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.APIServices;

public record CommonPrice(string ExchangeSymbol, decimal LastPrice, decimal BestBidPrice, decimal BestAskPrice);
public record ExchangeApiData(
    Dictionary<string, string> ActiveBaseCoinsBySymbols,
    IEnumerable<CommonPrice> Prices,
    IEnumerable<(string BaseAsset, IEnumerable<NetworkInfo>? ConvertedNetworks)> AssetsInfo
    );

public abstract class BaseCryptoExchange(ILogger logger) : ICryptoExchangeApiService
{
    public abstract ExchangeMarketType Type { get; }

    public virtual async Task<List<AssetData>?> GetAssetsDataAsync(CancellationToken cancellationToken)
    {
        ExchangeApiData? data = null;
        try
        {
            data = await FetchDataAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError($"Service {GetType()} threw: {ex}");
            return new List<AssetData>(0);
        }

        var activeSymbols = data.ActiveBaseCoinsBySymbols;
        var allPrices = data.Prices;
        var assets = data.AssetsInfo.ToDictionary(a => a.BaseAsset, a => a.ConvertedNetworks);

        var tradingPrices = new List<AssetData>();

        foreach (var ticker in allPrices)
        {
            string symbol = ticker.ExchangeSymbol;
            var lastPrice = ticker.LastPrice;

            if (activeSymbols.ContainsKey(symbol))
            {
                var baseAsset = activeSymbols[symbol];
                if (!assets.ContainsKey(baseAsset)) continue;
                var networks = assets[baseAsset];

                if (networks is null || !networks.Any()) continue;

                //TODO: temp
                string quote = "";
                if (symbol.Contains("USDT"))
                {
                    quote = "USDT";
                }
                if (symbol.Contains("USDС"))
                {
                    quote = "USDС";
                }
                if (symbol.Contains("BTC"))
                {
                    quote = "BTC";
                }
                var cryptoPrice = new AssetData(Type, symbol, quote, lastPrice, ticker.BestBidPrice, ticker.BestAskPrice, networks.ToList());
                tradingPrices.Add(cryptoPrice);
            }
        }

        return tradingPrices;
    }

    public async Task<(decimal AsksPercentage, decimal BidsPercentage)> GetOrderBooksAsync(string symbol, CancellationToken cancellationToken)
    {
        var data = await GetAsksBids(symbol, cancellationToken);
        var asksQuantity = data.Asks.Sum(x => x.Quantity);
        var bidsQuantity = data.Bids.Sum(x => x.Quantity);
        var fullQuantity = asksQuantity + bidsQuantity;

        var asksPercent = asksQuantity.Percentage(fullQuantity).RoundDecimals(1);
        var bidsPercent = bidsQuantity.Percentage(fullQuantity).RoundDecimals(1);

        //return Math.Abs(bidsPercent - asksQuantity);
        return (asksPercent, bidsPercent);
    }

    protected abstract Task<ExchangeApiData> FetchDataAsync(CancellationToken cancellationToken);

    protected abstract Task<(IEnumerable<ISymbolOrderBookEntry> Asks, IEnumerable<ISymbolOrderBookEntry> Bids)> GetAsksBids(string symbol, CancellationToken cancellationToken);
}
