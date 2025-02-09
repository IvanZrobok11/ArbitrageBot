using System.Text.Json.Serialization;

namespace BusinessLogic.Models;

//Symbol (торгова пара)
//BaseAsset(основна монета)
//QuoteAsset(валюта, у якій торгується)
//LastPrice(остання ціна)

public enum ExchangeType
{
    None, Binance, ByBit, KuCoin
}
public record CryptoPrice([property: JsonConverter(typeof(JsonStringEnumConverter))] ExchangeType Type, string Symbol, /*string BaseAsset, string QuoteAsset, */decimal LastPrice);

public class BybitResponse
{
    public BybitResult Result { get; set; }

}

public class BybitResult
{
    public string Category { get; set; }
    public List<BybitTicker> List { get; set; }

}

public class BybitTicker
{
    public string Symbol { get; set; }
    //[JsonProperty("lastPrice")]
    public decimal LastPrice { get; set; }
}


public class CryptoApiSettings
{
    public string BinanceUrl { get; set; }
    public string BybitUrl { get; set; }
}
