using BusinessLogic.Extensions;
using BusinessLogic.Interfaces;
using BusinessLogic.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BusinessLogic.Services;

public class CommonExchangeService
{
    private readonly IReadOnlyDictionary<ExchangeMarketType, ICryptoExchangeApiService> _cryptoApiServices;
    private readonly ILogger<CommonExchangeService> _logger;

    public CommonExchangeService(IEnumerable<ICryptoExchangeApiService> cryptoApiServices, ILogger<CommonExchangeService> logger, IConfiguration configuration)
    {
        _logger = logger;
        var allowedExchanges = configuration.GetSection("allowedExchanges").Get<ExchangeMarketType[]>()!;
        _cryptoApiServices = cryptoApiServices.Where(service => allowedExchanges.Contains(service.Type)).ToDictionary(s => s.Type, s => s);
    }

    public async IAsyncEnumerable<AssetsPair> GetAssetsPairsAsync(ushort minPercent, ushort maxPercent, bool matchNetworks, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        sw.Start();

        var tasks = _cryptoApiServices.Select(service => service.Value.GetAssetsDataAsync(cancellationToken));
        var assetsData = await Task.WhenAll(tasks);

        _logger.LogInformation($"WhenAll ElapsedMilliseconds:{sw.ElapsedMilliseconds}");
        sw.Restart();

        var groups = assetsData
            //.AsParallel() // more slowly with AsParallel
            .Where(asset => asset is not null)
            .SelectMany(_ => _ ?? new())
            //.Where(x => allowedExchanges.Contains(x.Type))
            .GroupBy(asset => asset.Symbol)
            .Where(group => group.Count() > 1);

        foreach (var group in groups)
        {
            var tickers = group.Where(x => x.LastPrice != 0).ToArray();

            // Choose all pairs in group where all ones tickers less than other
            foreach (var theLowestPriceOfTicker in tickers)
            {
                foreach (var ticker in tickers.Where(x => x.Type != theLowestPriceOfTicker.Type))
                {
                    if (ticker.LastPrice == 0 || theLowestPriceOfTicker.LastPrice == 0) continue;

                    var minMaxPercentCondition = ticker.LastPrice > theLowestPriceOfTicker.LastPrice.PercentOf(minPercent) + theLowestPriceOfTicker.LastPrice
                        && ticker.LastPrice < theLowestPriceOfTicker.LastPrice.PercentOf(maxPercent) + theLowestPriceOfTicker.LastPrice;
                    if (!minMaxPercentCondition) continue;

                    var intersectedNetworks = ticker.Networks.Select(n => n.Name).Intersect(theLowestPriceOfTicker.Networks.Select(n => n.Name));
                    if (matchNetworks && intersectedNetworks.Count() == 0) continue;

                    yield return new(theLowestPriceOfTicker, ticker);
                }
            }
        }
    }

    public async Task<List<AssetsPairViewModel>> GetSmartAssetPairsAsync(ushort minPercent, ushort maxPercent, bool matchNetworks, CancellationToken cancellationToken)
    {
        var diffPriceAssets = await GetAssetsPairsAsync(minPercent, maxPercent, matchNetworks, cancellationToken).ToListAsync(cancellationToken);
        var sw = new Stopwatch();
        sw.Start();

        return diffPriceAssets.AsParallel().Select(async diffPriceAsset =>
        {
            var assetToBuy = diffPriceAsset.LowPriceAsset;
            var assetToSell = diffPriceAsset.BigPriceAsset;
            var intersectedNetworks = assetToSell.Networks.Select(n => n.Name).Intersect(assetToBuy.Networks.Select(n => n.Name));
            var sw = new Stopwatch();
            sw.Start();

            _logger.LogWarning($"Start foreach async {assetToBuy.ExchangeSymbol}");

            var orderBookToBuyTask = _cryptoApiServices[assetToBuy.Type].GetOrderBooksAsync(assetToBuy.ExchangeSymbol, cancellationToken);
            var orderBookToSellTask = _cryptoApiServices[assetToSell.Type].GetOrderBooksAsync(assetToSell.ExchangeSymbol, cancellationToken);

            await Task.WhenAll(orderBookToBuyTask, orderBookToSellTask);

            var orderBookToBuy = orderBookToBuyTask.Result;
            var orderBookToSell = orderBookToSellTask.Result;

            _logger.LogWarning($"Finish foreach async {assetToBuy.ExchangeSymbol} " + sw.ElapsedMilliseconds);
            return intersectedNetworks.Select(networkName =>
            {
                var exchangeForBuy = new AssetExchangeInfo(assetToBuy.Type,
                    assetToBuy.LastPrice,
                    assetToBuy.Networks.First(x => x.Name == networkName),
                    orderBookToBuy.AsksPercentage,
                    orderBookToBuy.BidsPercentage);

                var exchangeForSell = new AssetExchangeInfo(assetToSell.Type,
                    assetToSell.LastPrice,
                    assetToSell.Networks.First(x => x.Name == networkName),
                    orderBookToSell.AsksPercentage,
                    orderBookToSell.BidsPercentage);

                return new AssetsPairViewModel(assetToSell.Symbol, diffPriceAsset.DiffPricePercent, exchangeForBuy, exchangeForSell);
            }).ToList();
        }).Select(x => x.Result).SelectMany(_ => _).ToList();
    }
}
