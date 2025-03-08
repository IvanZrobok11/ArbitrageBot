using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using BusinessLogic.Extensions;
using BusinessLogic.Models;
using CryptoExchange.Net.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ExchangeMarketType = BusinessLogic.Models.ExchangeMarketType;

namespace BusinessLogic.APIServices;

public class BinanceAPIService : BaseCryptoExchange
{
    private static BinanceRestClient _restClient = new BinanceRestClient((o) =>
    {
        o.RequestTimeout = TimeSpan.FromSeconds(5);
        o.RateLimiterEnabled = false;
        o.ReceiveWindow = TimeSpan.FromSeconds(10);
        o.CachingEnabled = true;
        o.CachingMaxAge = TimeSpan.FromSeconds(20);
    });
    public BinanceAPIService(ILogger<BinanceAPIService> logger, IOptionsSnapshot<CryptoAPISettings> options) : base(logger)
    {
        var apiCredentials = options.Value.ExchangesCredentials[Type];
        _restClient.SetApiCredentials(new CryptoExchange.Net.Authentication.ApiCredentials(apiCredentials.Key, apiCredentials.Secret));
    }

    public override ExchangeMarketType Type => ExchangeMarketType.Binance;

    protected override async Task<ExchangeApiData> FetchDataAsync(CancellationToken cancellationToken)
    {
        var exchangeInfoTask = _restClient.SpotApi.ExchangeData.GetExchangeInfoAsync(symbolStatus: Binance.Net.Enums.SymbolStatus.Trading, ct: cancellationToken); // api/v3/exchangeInfo?permissions=SPOT/ all symbols with baseCoin, quoteCoin, and Status
        var pricesTask = _restClient.SpotApi.ExchangeData.GetPricesAsync(cancellationToken); //api/v3/ticker/price // symbol and last price
        var userAssetsTask = _restClient.SpotApi.Account.GetUserAssetsAsync(ct: cancellationToken); // with network info

        await Task.WhenAll(exchangeInfoTask, pricesTask, userAssetsTask);

        var exchangeInfoResult = exchangeInfoTask.Result.ShouldSuccess();
        var pricesResult = pricesTask.Result.ShouldSuccess();
        var userAssetsResult = userAssetsTask.Result.ShouldSuccess();

        var activeSymbols = exchangeInfoResult.Data.Symbols
            .Where(x => x.Status == Binance.Net.Enums.SymbolStatus.Trading && x.IsSpotTradingAllowed)
            .ToDictionary(x => x.Name, x => x.BaseAsset); // price by symbol
        var allPrices = pricesResult.Data.Select(x => (x.Symbol, x.Price));
        var assets = GetConvertedAssets(userAssetsResult.Data);

        return new ExchangeApiData(activeSymbols, allPrices, assets);
    }

    protected override async Task<(IEnumerable<ISymbolOrderBookEntry> Asks, IEnumerable<ISymbolOrderBookEntry> Bids)> GetAsksBids(string symbol, CancellationToken cancellationToken)
    {
        var response = await _restClient.SpotApi.ExchangeData.GetOrderBookAsync(symbol, 1000, cancellationToken);
        response.ShouldSuccess();
        return (response.Data.Asks, response.Data.Bids);
    }

    private IEnumerable<(string BaseAsset, IEnumerable<NetworkInfo>? ConvertedNetworks)> GetConvertedAssets(IEnumerable<BinanceUserAsset> assets)
    {
        foreach (var asset in assets)
        {
            var networks = Enumerable.Empty<NetworkInfo>();
            foreach (var network in asset.NetworkList)
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