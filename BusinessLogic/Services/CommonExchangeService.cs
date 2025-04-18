using BusinessLogic.Extensions;
using BusinessLogic.Interfaces;
using BusinessLogic.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
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

    public async IAsyncEnumerable<AssetsPair> GetAssetsPairsAsync(decimal minPercent, decimal maxPercent, bool matchNetworks, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (minPercent > maxPercent)
        {
            throw new ArgumentException("Min value higher than max value");
        }
        if (minPercent < 0 || maxPercent < 0)
        {
            throw new ArgumentException("Values should be more than zero");
        }

        var sw = Stopwatch.StartNew();

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

    public async Task<List<AssetsPairViewModel>> GetSmartAssetPairsAsync(decimal minPercent, decimal maxPercent, CancellationToken cancellationToken)
    {
        // Fetch differing price assets
        var diffPriceAssets = await GetAssetsPairsAsync(minPercent, maxPercent, true, cancellationToken)
            .ToListAsync(cancellationToken);

        // Use Parallel.ForEachAsync for better async parallel processing
        var results = new ConcurrentBag<AssetsPairViewModel>();

        var sw = Stopwatch.StartNew();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 8,
            CancellationToken = cancellationToken
        };
        await Parallel.ForEachAsync(diffPriceAssets.AsEnumerable(), parallelOptions, async (diffPriceAsset, token) =>
        {
            try
            {
                var assetToBuy = diffPriceAsset.LowPriceAsset;
                var assetToSell = diffPriceAsset.BigPriceAsset;

                // Precompute intersected networks to avoid repeated computation
                var intersectedNetworks = assetToSell.Networks
                    .Select(n => n.Name)
                    .Intersect(assetToBuy.Networks.Select(n => n.Name))
                    .ToList();

                var orderBookToBuyTask = _cryptoApiServices[assetToBuy.Type]
                    .GetOrderBooksAsync(assetToBuy.ExchangeSymbol, token);

                var orderBookToSellTask = _cryptoApiServices[assetToSell.Type]
                    .GetOrderBooksAsync(assetToSell.ExchangeSymbol, token);

                await Task.WhenAll(orderBookToBuyTask, orderBookToSellTask);
                var orderBookToBuy = orderBookToBuyTask.Result;
                var orderBookToSell = orderBookToSellTask.Result;

                // Optimize network selection with null checks and FirstOrDefault
                var assetPairs = intersectedNetworks
                    .Select(networkName =>
                    {
                        var buyNetwork = assetToBuy.Networks.FirstOrDefault(x => x.Name == networkName);
                        var sellNetwork = assetToSell.Networks.FirstOrDefault(x => x.Name == networkName);

                        if (buyNetwork == null || sellNetwork == null) return null;

                        var exchangeForBuy = new AssetExchangeInfo(
                            assetToBuy.Type,
                            assetToBuy.LastPrice,
                            assetToBuy.BestBidPrice,
                            assetToBuy.BestAskPrice,
                            buyNetwork,
                            orderBookToBuy.AsksPercentage,
                            orderBookToBuy.BidsPercentage
                        );

                        var exchangeForSell = new AssetExchangeInfo(
                            assetToSell.Type,
                            assetToSell.LastPrice,
                            assetToSell.BestBidPrice,
                            assetToSell.BestAskPrice,
                            sellNetwork,
                            orderBookToSell.AsksPercentage,
                            orderBookToSell.BidsPercentage
                        );

                        return new AssetsPairViewModel(
                            assetToSell.Symbol,
                            assetToSell.QuoteAsset,
                            diffPriceAsset.DiffPricePercent,
                            exchangeForBuy,
                            exchangeForSell
                        );
                    })
                    .ToList();

                // Add results to thread-safe collection
                foreach (var pair in assetPairs)
                {
                    if (pair is not null) results.Add(pair);
                }
            }
            catch (Exception ex)
            {
                // Log or handle exceptions for individual asset processing
                _logger.LogError(ex, $"Error processing asset pair: {diffPriceAsset.LowPriceAsset.Symbol}");
            }
        });
        sw.Stop();
        _logger.LogInformation($"GetSmartAssetPairsAsync elapsed milliseconds: {sw.ElapsedMilliseconds}");
        return results.ToList();
    }
}
