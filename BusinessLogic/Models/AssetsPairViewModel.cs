namespace BusinessLogic.Models;

public record AssetsPairViewModel(string Symbol, decimal DiffPercent, AssetExchangeInfo ExchangeForBuy, AssetExchangeInfo ExchangeForSell)
{
    public List<AssetStats> Stats { get; } = new List<AssetStats>
    {
        new(100, ExchangeForBuy, ExchangeForSell, DiffPercent) ,
        new(500, ExchangeForBuy, ExchangeForSell, DiffPercent) ,
        new(1000, ExchangeForBuy, ExchangeForSell, DiffPercent) ,
    };

    public AssetStats GetStats(int budget) => new AssetStats(budget, ExchangeForBuy, ExchangeForSell, DiffPercent);
}
