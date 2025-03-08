using BusinessLogic.Extensions;
using BusinessLogic.Models;
using CryptoExchange.Net.Interfaces;
using GateIo.Net.Clients;
using GateIo.Net.Objects.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusinessLogic.APIServices;

public class GateIoAPIService : BaseCryptoExchange
{
    private static GateIoRestClient _restClient = new GateIoRestClient((o) =>
    {
        o.RequestTimeout = TimeSpan.FromSeconds(10);
        o.RateLimiterEnabled = false;
    });

    public GateIoAPIService(ILogger<GateIoAPIService> logger, IOptionsSnapshot<CryptoAPISettings> options) : base(logger)
    {
        var apiCredentials = options.Value.ExchangesCredentials[Type];
        _restClient.SetApiCredentials(new CryptoExchange.Net.Authentication.ApiCredentials(apiCredentials.Key, apiCredentials.Secret));
    }

    public override ExchangeMarketType Type => ExchangeMarketType.GateIo;

    protected override async Task<ExchangeApiData> FetchDataAsync(CancellationToken cancellationToken)
    {
        var symbolsTask = _restClient.SpotApi.ExchangeData.GetSymbolsAsync(cancellationToken); // spot/currency_pairs // all symbols with baseCoin, quoteCoin, and Status
        var tickersTask = _restClient.SpotApi.ExchangeData.GetTickersAsync(ct: cancellationToken); // spot/tickers // symbol and last price
        var userAssetsTask = _restClient.SpotApi.ExchangeData.GetAssetsAsync(cancellationToken); // with network info

        await Task.WhenAll(symbolsTask, tickersTask, userAssetsTask);

        var symbolsResponse = symbolsTask.Result.ShouldSuccess();
        var tickersResult = tickersTask.Result.ShouldSuccess();
        var userAssetsResult = userAssetsTask.Result.ShouldSuccess();

        var activeSymbols = symbolsResponse.Data
            .Where(x => x.TradeStatus == GateIo.Net.Enums.SymbolStatus.Tradable)
            .ToDictionary(x => x.Name, x => x.BaseAsset);

        var prices = tickersResult.Data.Select(x => (x.Symbol, x.LastPrice));
        var assets = GetConvertedAssets(userAssetsResult.Data);

        return new ExchangeApiData(activeSymbols, prices, assets);
    }

    protected override Task<(IEnumerable<ISymbolOrderBookEntry> Asks, IEnumerable<ISymbolOrderBookEntry> Bids)> GetAsksBids(string symbol, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private IEnumerable<(string BaseAsset, IEnumerable<NetworkInfo>? ConvertedNetworks)> GetConvertedAssets(IEnumerable<GateIoAsset> assets)
    {
        foreach (var asset in assets)
        {
            if (asset?.Network is null) continue;
            if (asset.Delisted || asset.WithdrawDisabled || asset.WithdrawDelayed || asset.DepositDisabled || asset.TradeDisabled) continue;

            yield return new(asset.Asset, [new NetworkInfo(asset.Network, asset.FixedFeeRate ?? -1, -1, -1, -1, -1)]);
        }
    }
}