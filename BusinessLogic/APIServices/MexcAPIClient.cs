using BusinessLogic.Extensions;
using BusinessLogic.Models;
using CryptoExchange.Net.Interfaces;
using Mexc.Net.Clients;
using Mexc.Net.Enums;
using Mexc.Net.Objects.Models.Spot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusinessLogic.APIServices;

public class MexcAPIClient : BaseCryptoExchange
{
    public static MexcRestClient restClient = new MexcRestClient((o) =>
    {
        o.RequestTimeout = TimeSpan.FromSeconds(5);
        o.RateLimiterEnabled = false;
        o.ReceiveWindow = TimeSpan.FromSeconds(10);
        o.CachingEnabled = true;
        o.CachingMaxAge = TimeSpan.FromSeconds(20);
    });
    public MexcAPIClient(ILogger<MexcAPIClient> logger, IOptionsSnapshot<CryptoAPISettings> options) : base(logger)
    {
        var apiCredentials = options.Value.ExchangesCredentials[Type];
        restClient.SetApiCredentials(new CryptoExchange.Net.Authentication.ApiCredentials(apiCredentials.Key, apiCredentials.Secret));
    }

    public override ExchangeMarketType Type => ExchangeMarketType.Mexc;

    protected override async Task<ExchangeApiData> FetchDataAsync(CancellationToken cancellationToken)
    {
        var exchangeInfoTask = restClient.SpotApi.ExchangeData.GetExchangeInfoAsync(ct: cancellationToken); // api/v3/exchangeInfo// all symbols with baseCoin, quoteCoin, and Status
        var pricesTask = restClient.SpotApi.ExchangeData.GetPricesAsync(); // spot/tickers // symbol and last price
        var userAssetsTask = restClient.SpotApi.Account.GetUserAssetsAsync(); // with network info

        await Task.WhenAll(exchangeInfoTask, pricesTask, userAssetsTask);

        var exchangeInfoResult = exchangeInfoTask.Result.ShouldSuccess();
        var pricesResult = pricesTask.Result.ShouldSuccess();
        var userAssetsResult = userAssetsTask.Result.ShouldSuccess();

        var activeSymbols = exchangeInfoResult.Data.Symbols
            .Where(x => x.Status == SymbolStatus.Enabled && x.IsSpotTradingAllowed)
            .ToDictionary(x => x.Name, x => x.BaseAsset); // price by symbol
        var allPrices = pricesTask.Result.Data.Select(x => (x.Symbol, x.Price));
        var assets = GetConvertedAssets(userAssetsTask.Result.Data);

        return new ExchangeApiData(activeSymbols, allPrices, assets);
    }

    protected override async Task<(IEnumerable<ISymbolOrderBookEntry> Asks, IEnumerable<ISymbolOrderBookEntry> Bids)> GetAsksBids(string symbol, CancellationToken cancellationToken)
    {
        //limit: default 100, max 5000
        var response = await restClient.SpotApi.ExchangeData.GetOrderBookAsync(symbol, 1000, cancellationToken);
        response.ShouldSuccess();
        return (response.Data.Asks, response.Data.Bids);
    }

    private IEnumerable<(string BaseAsset, IEnumerable<NetworkInfo>? ConvertedNetworks)> GetConvertedAssets(IEnumerable<MexcUserAsset> assets)
    {
        foreach (var asset in assets.Where(a => a.Networks is not null))
        {
            var networks = Enumerable.Empty<NetworkInfo>();
            foreach (var network in asset.Networks)
            {
                if (!network.DepositEnabled || !network.WithdrawEnabled) continue;
                networks = networks.Append(new NetworkInfo(
                    network.Network,
                    network.WithdrawFee,
                    -1,
                    -1,
                    network.WithdrawMin,
                    network.WithdrawMax));
            }

            if (!networks.Any()) continue;
            yield return new(asset.Asset, networks);
        }
    }
}