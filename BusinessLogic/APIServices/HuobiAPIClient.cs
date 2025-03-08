using BusinessLogic.Extensions;
using BusinessLogic.Models;
using CryptoExchange.Net.Interfaces;
using Huobi.Net.Clients;
using Huobi.Net.Enums;
using Huobi.Net.Objects.Models;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.APIServices
{
    public class HuobiAPIClient : BaseCryptoExchange
    {
        private static HuobiRestClient restClient = new HuobiRestClient((o) =>
        {
            o.RequestTimeout = TimeSpan.FromSeconds(10);
            o.RateLimiterEnabled = false;
        });
        public HuobiAPIClient(ILogger<HuobiAPIClient> logger) : base(logger)
        {
            //_restClient.SetApiCredentials(new CryptoExchange.Net.Authentication.ApiCredentials());
        }

        public override ExchangeMarketType Type => ExchangeMarketType.Huobi;

        protected override async Task<ExchangeApiData> FetchDataAsync(CancellationToken cancellationToken)
        {
            var spotSymbolsTask = restClient.SpotApi.ExchangeData.GetSymbolsAsync(ct: cancellationToken);
            var spotTickersTask = restClient.SpotApi.ExchangeData.GetTickersAsync(ct: cancellationToken); // symbol and last price
            var assetInfoTask = restClient.SpotApi.ExchangeData.GetAssetDetailsAsync(ct: cancellationToken); // with network info

            await Task.WhenAll(spotSymbolsTask, spotTickersTask, assetInfoTask);

            var spotSymbolsResult = spotSymbolsTask.Result.ShouldSuccess();
            var spotTickersResult = spotTickersTask.Result.ShouldSuccess();
            var assetInfoResult = assetInfoTask.Result.ShouldSuccess();

            var activeSymbols = spotSymbolsResult.Data
                .Where(x => x.State == Huobi.Net.Enums.SymbolState.Online)
                .ToDictionary(x => x.Name, x => x.BaseAsset);

            var allPrices = spotTickersResult.Data.Ticks.Select(x => (x.Symbol, x.LastTradePrice));

            var assets = GetConvertedAssets(assetInfoResult.Data);
            return new ExchangeApiData(activeSymbols, allPrices, assets);
        }

        private IEnumerable<(string BaseAsset, IEnumerable<NetworkInfo>? ConvertedNetworks)> GetConvertedAssets(IEnumerable<HuobiAssetInfo> assets)
        {
            foreach (var asset in assets)
            {
                var networks = Enumerable.Empty<NetworkInfo>();
                foreach (var network in asset.Networks)
                {
                    //The chain status of deposit. false: suspend
                    if (network.DepositStatus != CurrencyStatus.Allowed || network.DepositStatus != CurrencyStatus.Allowed) continue;

                    decimal fee = 0;
                    // Maximum withdraw fee in each request (only applicable to withdrawFeeType = circulated or ratio)
                    if (network.WithdrawFeeType == FeeType.Circulated || network.WithdrawFeeType == FeeType.Ratio)
                    {
                        fee = network.MaxTransactFeeWithdraw;
                    }
                    else if (network.WithdrawFeeType == FeeType.Fixed)
                    {
                        fee = network.TransactFeeWithdraw;
                    }

                    networks = networks.Append(new NetworkInfo(network.BaseChain,
                        fee,
                        network.TransactFeeRateWithdraw is null ? network.TransactFeeRateWithdraw : network.TransactFeeRateWithdraw.Value * 100,
                        network.MinDepositQuantity,
                        network.MinWithdrawQuantity,
                        network.MaxWithdrawQuantity));
                }
                if (!networks.Any()) continue;
                yield return new(asset.Asset, networks);
            }
        }

        protected override async Task<(IEnumerable<ISymbolOrderBookEntry> Asks, IEnumerable<ISymbolOrderBookEntry> Bids)> GetAsksBids(string symbol, CancellationToken cancellationToken)
        {
            // limit: 5, 10, 20
            // step: 0 - 6
            var response = await restClient.SpotApi.ExchangeData.GetOrderBookAsync(symbol, 1, 20, cancellationToken);
            response.ShouldSuccess();

            return (response.Data.Asks, response.Data.Bids);
        }
    }
}
