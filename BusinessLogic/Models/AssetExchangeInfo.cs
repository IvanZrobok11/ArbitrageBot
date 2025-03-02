using BusinessLogic.Extensions;
using System.Text.Json.Serialization;

namespace BusinessLogic.Models;

public record AssetExchangeInfo(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ExchangeMarketType Type,
    [property: JsonIgnore] decimal Price,
    NetworkInfo Network,
    decimal AsksPercentage /*want to sell*/,
    decimal BidsPercentage /*want to buy*/)
{
    [JsonInclude, JsonPropertyName("price")]
    public decimal ViewPrice => Price.RoundDecimals(7, MidpointRounding.AwayFromZero);

    public decimal LiquidityPercentage => 100 - Math.Abs(AsksPercentage - BidsPercentage);
}
