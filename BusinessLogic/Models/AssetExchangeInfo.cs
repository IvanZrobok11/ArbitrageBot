using BusinessLogic.Extensions;
using System.Text.Json.Serialization;

namespace BusinessLogic.Models;

public record AssetExchangeInfo(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ExchangeMarketType Type,
    [property: JsonIgnore] decimal LastPrice,
    [property: JsonIgnore] decimal BestBidPrice,
    [property: JsonIgnore] decimal BestAskPrice,
    NetworkInfo Network,
    decimal WantToSellPercentage /*want to sell*/,
    decimal WantToBuyPercentage /*want to buy*/)
{
    [JsonInclude, JsonPropertyName("last_price")]
    public decimal ViewLastPrice => LastPrice.RoundDecimals(7, MidpointRounding.AwayFromZero);

    [JsonInclude, JsonPropertyName("best_bid_price")]
    public decimal ViewBestBidPrice => BestBidPrice.RoundDecimals(7, MidpointRounding.AwayFromZero);

    [JsonInclude, JsonPropertyName("best_ask_price")]
    public decimal ViewBestAskPrice => BestAskPrice.RoundDecimals(7, MidpointRounding.AwayFromZero);

    public decimal LiquidityPercentage => 100 - Math.Abs(WantToSellPercentage - WantToBuyPercentage);
}
