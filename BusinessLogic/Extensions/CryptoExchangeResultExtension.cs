using CryptoExchange.Net.Objects;

namespace BusinessLogic.Extensions;

public static class CryptoExchangeResultExtension
{
    public static WebCallResult<T> ShouldSuccess<T>(this WebCallResult<T> result)
    {
        if (!result.Success)
        {
            throw new Exception($"Web error: {result.Error}");
        }
        return result;
    }
}
