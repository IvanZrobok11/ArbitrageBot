using BusinessLogic.Models;

namespace BusinessLogic.Interfaces;
public interface ICryptoExchangeApiService
{
    Task<List<AssetData>?> GetAssetsDataAsync(CancellationToken cancellationToken);
    Task<(decimal AsksPercentage, decimal BidsPercentage)> GetOrderBooksAsync(string symbol, CancellationToken cancellationToken);
    ExchangeMarketType Type { get; }
}
