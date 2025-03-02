using System.Text.Json.Serialization;

namespace BusinessLogic.Models;

public record AssetData(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ExchangeMarketType Type,
    string ExchangeSymbol,
    decimal LastPrice,
    List<NetworkInfo> Networks)
{
    [JsonInclude]
    public string Symbol => ExchangeSymbol.Replace("_", "").Replace("-", "");

    [JsonIgnore]
    internal string ExchangeSymbol = ExchangeSymbol;

    public List<decimal>? WithdrawFullFees
    {
        get
        {
            if (Networks == null) return null;
            return Networks.Select(n => n.GetWithdrawFullFee(LastPrice)).ToList();
        }
    }
}
