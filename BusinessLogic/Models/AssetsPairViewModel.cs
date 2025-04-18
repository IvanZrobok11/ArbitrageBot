namespace BusinessLogic.Models;

public record AssetsPairViewModel(string Symbol, string Quote, decimal DiffPercent, AssetExchangeInfo ExchangeForBuy, AssetExchangeInfo ExchangeForSell)
{
    public List<AssetStats> Stats { get; } = new List<AssetStats>
    {
        new(Quote,100, ExchangeForBuy, ExchangeForSell, DiffPercent) ,
        new(Quote,500, ExchangeForBuy, ExchangeForSell, DiffPercent) ,
        new(Quote,1000, ExchangeForBuy, ExchangeForSell, DiffPercent) ,
    };

    public AssetStats GetStats(int budget) => new AssetStats(Quote, budget, ExchangeForBuy, ExchangeForSell, DiffPercent);
}
