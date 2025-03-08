using BusinessLogic.Extensions;
using BusinessLogic.Models;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using CryptoExchange.Net.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusinessLogic.APIServices;

public class ByBitAPIService : BaseCryptoExchange
{
    private static BybitRestClient restClient = new BybitRestClient((o) =>
    {
        o.RequestTimeout = TimeSpan.FromSeconds(10);
        o.RateLimiterEnabled = false;
        o.ReceiveWindow = TimeSpan.FromSeconds(10);
        o.CachingEnabled = true;
        o.CachingMaxAge = TimeSpan.FromSeconds(20);
    });
    public ByBitAPIService(ILogger<ByBitAPIService> logger, IOptionsSnapshot<CryptoAPISettings> options) : base(logger)
    {
        var apiCredentials = options.Value.ExchangesCredentials[Type];
        restClient.SetApiCredentials(new CryptoExchange.Net.Authentication.ApiCredentials(apiCredentials.Key, apiCredentials.Secret));
    }

    public override ExchangeMarketType Type => ExchangeMarketType.ByBit;

    protected override async Task<ExchangeApiData> FetchDataAsync(CancellationToken cancellationToken)
    {
        var spotSymbolsTask = restClient.V5Api.ExchangeData.GetSpotSymbolsAsync(ct: cancellationToken); // all symbols with baseCoin, quoteCoin, and Status
        var spotTickersTask = restClient.V5Api.ExchangeData.GetSpotTickersAsync(ct: cancellationToken); // symbol and last price
        var assetInfoTask = restClient.V5Api.Account.GetAssetInfoAsync(ct: cancellationToken); // with network info

        await Task.WhenAll(spotSymbolsTask, spotTickersTask, assetInfoTask);

        var spotSymbolsResult = spotSymbolsTask.Result.ShouldSuccess();
        var spotTickersResult = spotTickersTask.Result.ShouldSuccess();
        var assetInfoResult = assetInfoTask.Result.ShouldSuccess();

        var activeSymbols = spotSymbolsResult.Data.List
            .Where(x => x.Status == Bybit.Net.Enums.SymbolStatus.Trading)
            .ToDictionary(x => x.Name, x => x.BaseAsset);

        var allPrices = spotTickersResult.Data.List.Select(x => (x.Symbol, x.LastPrice));

        var assets = GetConvertedAssets(assetInfoResult.Data.Assets);
        return new ExchangeApiData(activeSymbols, allPrices, assets);
    }

    private IEnumerable<(string BaseAsset, IEnumerable<NetworkInfo>? ConvertedNetworks)> GetConvertedAssets(IEnumerable<BybitUserAssetInfo> assets)
    {
        foreach (var asset in assets)
        {
            var networks = Enumerable.Empty<NetworkInfo>();
            foreach (var network in asset.Networks)
            {
                //withdraw fee. If withdraw fee is empty, It means that this coin does not support withdrawal
                if (network.WithdrawFee is null) continue;

                //The chain status of deposit. false: suspend
                if (network.NetworkDeposit is null || network.NetworkWithdraw is null || !network.NetworkDeposit.Value || !network.NetworkWithdraw.Value) continue;

                var maximumWithdrawAmountPerTransaction = asset.QuantityRemaining;
                networks = networks.Append(new NetworkInfo(network.Network,
                    network.WithdrawFee.Value,
                    network.WithdrawPercentageFee,
                    network.MinDeposit,
                    network.MinWithdraw,
                    maximumWithdrawAmountPerTransaction));
            }
            if (!networks.Any()) continue;
            yield return new(asset.Asset, networks);
        }
    }

    protected override async Task<(IEnumerable<ISymbolOrderBookEntry> Asks, IEnumerable<ISymbolOrderBookEntry> Bids)> GetAsksBids(string symbol, CancellationToken cancellationToken)
    {
        //limit: 1-200
        var response = await restClient.V5Api.ExchangeData.GetOrderbookAsync(Category.Spot, symbol, 200, cancellationToken);
        response.ShouldSuccess();

        return (response.Data.Asks, response.Data.Bids);
    }
}