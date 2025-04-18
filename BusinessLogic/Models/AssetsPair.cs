using System.Text.Json.Serialization;

namespace BusinessLogic.Models;

public record AssetsPair(AssetData LowPriceAsset, AssetData BigPriceAsset)
{
    [JsonInclude]
    public decimal DiffPricePercent
    {
        get
        {
            var diffPercent = ((BigPriceAsset.LastPrice - LowPriceAsset.LastPrice) / LowPriceAsset.LastPrice) * 100;
            return Math.Round(diffPercent, 3, MidpointRounding.ToEven);
        }
    }
}
