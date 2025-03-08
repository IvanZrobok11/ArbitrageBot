using BusinessLogic.Extensions;
using System.Text.Json.Serialization;

namespace BusinessLogic.Models;

public class AssetStats
{
    [JsonInclude] public readonly decimal USDTBudget;
    [JsonInclude] public readonly decimal FixedWithdrawFee; // ціна фіксованого податку в USDT 
    [JsonInclude] public readonly decimal DynamicWithdrawForUSDTBudgetFee; // ціна податку для 100 USDT    
    [JsonInclude] public decimal Fees => FixedWithdrawFee + DynamicWithdrawForUSDTBudgetFee;
    [JsonInclude] public readonly decimal? MinSellPrice; // На яку суму мінімально можна продати за 1 раз
    [JsonInclude] public readonly decimal? MaxSellPrice; // На яку суму максимально можна продати за 1 раз
    [JsonInclude] public readonly decimal USDTProfit; // скільки отримаю за переказ на 100 USDT
    public AssetStats(decimal uSDTBudget, AssetExchangeInfo exchangeToBuy, AssetExchangeInfo exchangeToSell, decimal diffPercent)
    {
        USDTBudget = uSDTBudget;
        FixedWithdrawFee += GetWithdrawStaticFee(exchangeToBuy.Network.WithdrawFee, exchangeToBuy.Price);
        FixedWithdrawFee += GetWithdrawStaticFee(exchangeToSell.Network.WithdrawFee, exchangeToSell.Price);

        DynamicWithdrawForUSDTBudgetFee = 0;
        if (exchangeToBuy.Network.WithdrawPercentageFee.HasValue && exchangeToBuy.Network.WithdrawPercentageFee >= 0)
        {
            DynamicWithdrawForUSDTBudgetFee += GetWithdrawDynamicFee(exchangeToBuy.Price, exchangeToBuy.Network.WithdrawPercentageFee.Value);
        }
        if (exchangeToSell.Network.WithdrawPercentageFee.HasValue && exchangeToSell.Network.WithdrawPercentageFee >= 0)
        {
            DynamicWithdrawForUSDTBudgetFee += GetWithdrawDynamicFee(exchangeToSell.Price, exchangeToSell.Network.WithdrawPercentageFee.Value);
        }

        if (exchangeToSell.Network.WithdrawMinSize is not null && exchangeToSell.Network.WithdrawMinSize.Value >= 0)
        {
            MinSellPrice = exchangeToSell.Network.WithdrawMinSize.Value * exchangeToSell.Price;
        }
        if (exchangeToSell.Network.WithdrawMaxSize is not null && exchangeToSell.Network.WithdrawMaxSize.Value >= 0)
        {
            MaxSellPrice = exchangeToSell.Network.WithdrawMaxSize.Value * exchangeToSell.Price;
        }

        USDTProfit = GetUSDTProfit(FixedWithdrawFee, DynamicWithdrawForUSDTBudgetFee, diffPercent);
    }

    private decimal GetUSDTProfit(decimal staticWithdrawFee, decimal percentWithdrawFee, decimal diffPercent)
    {
        return USDTBudget.PercentOf(diffPercent) - staticWithdrawFee - percentWithdrawFee;
    }

    private decimal GetWithdrawStaticFee(decimal price, decimal fee)
    {
        return price * fee;
    }

    private decimal GetWithdrawDynamicFee(decimal price, decimal feePercent)
    {
        return USDTBudget.PercentOf(feePercent);
    }
}