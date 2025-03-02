namespace BusinessLogic.Models;

//Symbol (торгова пара)
//BaseAsset(основна монета)
//QuoteAsset(валюта, у якій торгується)
//LastPrice(остання ціна)

public enum ExchangeMarketType
{
    None, Binance, ByBit, KuCoin, Mexc, GateIo
}
