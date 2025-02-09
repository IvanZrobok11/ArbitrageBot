using BusinessLogic.Models;

namespace BusinessLogic.Interfaces;
public interface ICryptoExchangeApiService
{
    Task<List<CryptoPrice>?> GetPricesAsync(CancellationToken cancellationToken);
}
