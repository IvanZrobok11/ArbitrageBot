using BusinessLogic.Extensions;
using BusinessLogic.Models;
using CryptoExchange.Net.Interfaces;
using Kucoin.Net.Clients;
using Kucoin.Net.Objects.Models.Spot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusinessLogic.APIServices;

public class KuCoinAPIService : BaseCryptoExchange
{
    public static KucoinRestClient restClient = new KucoinRestClient((o) =>
    {
        o.RequestTimeout = TimeSpan.FromSeconds(10);
        o.RateLimiterEnabled = false;
        o.CachingEnabled = true;
        o.CachingMaxAge = TimeSpan.FromSeconds(20);
    });
    public KuCoinAPIService(ILogger<KuCoinAPIService> logger, IOptionsSnapshot<CryptoAPISettings> options) : base(logger)
    {
        // restClient.SetApiCredentials(new Kucoin.Net.Objects.KucoinApiCredentials("", "", ""));
    }
    public override ExchangeMarketType Type => ExchangeMarketType.KuCoin;

    protected override async Task<ExchangeApiData> FetchDataAsync(CancellationToken cancellationToken)
    {
        var symbolsTask = restClient.SpotApi.ExchangeData.GetSymbolsAsync(ct: cancellationToken); // /api/v2/symbols// all symbols with baseCoin, quoteCoin, and Status
        var tickersTask = restClient.SpotApi.ExchangeData.GetTickersAsync(ct: cancellationToken); // /api/v1/market/allTickers // symbol and last price
        var assetsTask = restClient.SpotApi.ExchangeData.GetAssetsAsync(ct: cancellationToken); // api/v3/currencies with network info
        await Task.WhenAll(symbolsTask, tickersTask, assetsTask);

        var symbolsResult = symbolsTask.Result.ShouldSuccess();
        var tickersResult = tickersTask.Result.ShouldSuccess();
        var assetsResult = assetsTask.Result.ShouldSuccess();

        var activeSymbols = symbolsResult.Data.Where(x => x.EnableTrading).ToDictionary(x => x.Name, x => x.BaseAsset); // price by symbol
        var allPrices = tickersResult.Data.Data.Where(t => t.LastPrice.HasValue).Select(x => (x.Symbol, x.LastPrice!.Value));
        var assets = GetConvertedAssets(assetsResult.Data);
        return new ExchangeApiData(activeSymbols, allPrices, assets);
    }

    private IEnumerable<(string BaseAsset, IEnumerable<NetworkInfo>? ConvertedNetworks)> GetConvertedAssets(IEnumerable<KucoinAssetDetails> assets)
    {
        foreach (var asset in assets.Where(a => a.Networks is not null))
        {
            var networks = Enumerable.Empty<NetworkInfo>();
            foreach (var network in asset.Networks)
            {
                if (!network.IsDepositEnabled || !network.IsWithdrawEnabled) continue;
                networks = networks.Append(new NetworkInfo(network.NetworkId.ToUpper(),
                    network.WithdrawMaxFee ?? network.WithdrawalMinFee, // TODO: maybe better take average fee
                    network.WithdrawFeeRate,
                    network.DepositMinQuantity,
                    network.WithdrawalMinQuantity,
                    network.MaxWithdraw));
            }

            if (!networks.Any()) continue;
            yield return new(asset.Asset, networks);
        }
    }

    protected override async Task<(IEnumerable<ISymbolOrderBookEntry> Asks, IEnumerable<ISymbolOrderBookEntry> Bids)> GetAsksBids(string symbol, CancellationToken cancellationToken)
    {
        var response = await restClient.SpotApi.ExchangeData.GetAggregatedPartialOrderBookAsync(symbol, 100, cancellationToken);
        response.ShouldSuccess();
        return (response.Data.Asks, response.Data.Bids);
    }
}
