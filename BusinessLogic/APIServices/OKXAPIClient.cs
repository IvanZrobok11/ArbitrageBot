using BusinessLogic.Extensions;
using BusinessLogic.Models;
using CryptoExchange.Net.Interfaces;
using Microsoft.Extensions.Logging;
using OKX.Net.Clients;
using OKX.Net.Objects.Funding;

namespace BusinessLogic.APIServices
{
    public class OKXAPIClient : BaseCryptoExchange
    {
        public static OKXRestClient _restClient = new OKXRestClient((o) =>
        {
            o.RequestTimeout = TimeSpan.FromSeconds(10);
            o.RateLimiterEnabled = false;
        });
        public OKXAPIClient(ILogger<OKXAPIClient> logger) : base(logger)
        {
            //TODO
            //_restClient.SetApiCredentials(new OKX.Net.Objects.OKXApiCredentials(,));
        }

        public override ExchangeMarketType Type => ExchangeMarketType.OKX;

        protected override async Task<ExchangeApiData> FetchDataAsync(CancellationToken cancellationToken)
        {
            var exchangeInfoTask = _restClient.UnifiedApi.ExchangeData.GetSymbolsAsync(OKX.Net.Enums.InstrumentType.Spot, ct: cancellationToken);
            var pricesTask = _restClient.UnifiedApi.ExchangeData.GetTickersAsync(OKX.Net.Enums.InstrumentType.Spot, ct: cancellationToken);
            var userAssetsTask = _restClient.UnifiedApi.Account.GetAssetsAsync(ct: cancellationToken); // with network info

            await Task.WhenAll(exchangeInfoTask, pricesTask, userAssetsTask);

            var exchangeInfoResult = exchangeInfoTask.Result.ShouldSuccess();
            var pricesResult = pricesTask.Result.ShouldSuccess();
            var userAssetsResult = userAssetsTask.Result.ShouldSuccess();

            var activeSymbols = exchangeInfoResult.Data
                .Where(x => x.State == OKX.Net.Enums.InstrumentState.Live)
                .ToDictionary(x => x.Symbol, x => x.BaseAsset); // price by symbol
            var allPrices = pricesResult.Data.Where(x => x.LastPrice is not null).Select(x => (x.Symbol, x.LastPrice!.Value));
            var assets = GetConvertedAssets(userAssetsResult.Data);

            return new ExchangeApiData(activeSymbols, allPrices, assets);
        }

        private IEnumerable<(string BaseAsset, IEnumerable<NetworkInfo>? ConvertedNetworks)> GetConvertedAssets(IEnumerable<OKXAsset> assets)
        {
            foreach (var asset in assets)
            {
                if (asset.Network is null) continue;

                //The chain status of deposit. false: suspend
                if (!asset.AllowDeposit || !asset.AllowWithdrawal || !asset.AllowInternalTransfer) continue;

                var network = new List<NetworkInfo>
                {
                    new (asset.Network,
                    asset.FixedWithdrawalFee,
                    asset.BurningFeeRate is null ? asset.BurningFeeRate : asset.BurningFeeRate.Value * 100,
                    asset.MinDeposit, // TODO: Max deposit
                    asset.MinimumWithdrawalAmount,
                    asset.MaxWithdrawal)
                };

                yield return new(asset.Asset, network);
            }
        }

        protected override async Task<(IEnumerable<ISymbolOrderBookEntry> Asks, IEnumerable<ISymbolOrderBookEntry> Bids)> GetAsksBids(string symbol, CancellationToken cancellationToken)
        {
            // depth 1 - 400
            var response = await _restClient.UnifiedApi.ExchangeData.GetOrderBookAsync(symbol, 400, cancellationToken);
            response.ShouldSuccess();
            return (response.Data.Asks, response.Data.Bids);
        }
    }
}
