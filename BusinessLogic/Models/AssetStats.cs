using BusinessLogic.Extensions;
using System.Text.Json.Serialization;

namespace BusinessLogic.Models;

public class AssetStats
{
    [JsonInclude] public readonly string BudgetCurrency;
    [JsonInclude] public readonly decimal Budget;
    [JsonInclude] public readonly decimal FixedWithdrawFee; // ціна фіксованого податку в BudgetCurrency 
    [JsonInclude] public readonly decimal DynamicWithdrawForCurrencyBudgetFee; // ціна податку для 100 BudgetCurrency    
    [JsonInclude] public decimal Fees => FixedWithdrawFee + DynamicWithdrawForCurrencyBudgetFee;

    [JsonInclude] public readonly decimal? MinBuyWithdrawPrice; // На яку суму мінімально можна продати за 1 раз
    [JsonInclude] public readonly decimal? MaxBuyWithdrawPrice; // На яку суму максимально можна продати за 1 раз

    [JsonInclude] public readonly decimal? MinSellWithdrawPrice; // На яку суму мінімально можна продати за 1 раз
    [JsonInclude] public readonly decimal? MaxSellWithdrawPrice; // На яку суму максимально можна продати за 1 раз

    [JsonInclude] public readonly decimal Profit; // скільки отримаю за переказ на 100 BudgetCurrency
    public AssetStats(string budgetCurrency, decimal budget, AssetExchangeInfo exchangeToBuy, AssetExchangeInfo exchangeToSell, decimal diffPercent)
    {
        BudgetCurrency = budgetCurrency;
        Budget = budget;
        FixedWithdrawFee += GetWithdrawStaticFee(exchangeToBuy.Network.WithdrawFee, exchangeToBuy.LastPrice);
        FixedWithdrawFee += GetWithdrawStaticFee(exchangeToSell.Network.WithdrawFee, exchangeToSell.LastPrice);

        DynamicWithdrawForCurrencyBudgetFee = 0;
        if (exchangeToBuy.Network.WithdrawPercentageFee.HasValue && exchangeToBuy.Network.WithdrawPercentageFee >= 0)
        {
            DynamicWithdrawForCurrencyBudgetFee += GetWithdrawDynamicFee(exchangeToBuy.LastPrice, exchangeToBuy.Network.WithdrawPercentageFee.Value);
        }
        if (exchangeToSell.Network.WithdrawPercentageFee.HasValue && exchangeToSell.Network.WithdrawPercentageFee >= 0)
        {
            DynamicWithdrawForCurrencyBudgetFee += GetWithdrawDynamicFee(exchangeToSell.LastPrice, exchangeToSell.Network.WithdrawPercentageFee.Value);
        }

        if (exchangeToBuy.Network.WithdrawMinSize is not null && exchangeToBuy.Network.WithdrawMinSize.Value >= 0)
        {
            MinBuyWithdrawPrice = exchangeToBuy.Network.WithdrawMinSize.Value * exchangeToBuy.LastPrice;
        }
        if (exchangeToBuy.Network.WithdrawMaxSize is not null && exchangeToBuy.Network.WithdrawMaxSize.Value >= 0)
        {
            MaxBuyWithdrawPrice = exchangeToBuy.Network.WithdrawMaxSize.Value * exchangeToBuy.LastPrice;
        }

        if (exchangeToSell.Network.WithdrawMinSize is not null && exchangeToSell.Network.WithdrawMinSize.Value >= 0)
        {
            MinSellWithdrawPrice = exchangeToSell.Network.WithdrawMinSize.Value * exchangeToSell.LastPrice;
        }
        if (exchangeToSell.Network.WithdrawMaxSize is not null && exchangeToSell.Network.WithdrawMaxSize.Value >= 0)
        {
            MaxSellWithdrawPrice = exchangeToSell.Network.WithdrawMaxSize.Value * exchangeToSell.LastPrice;
        }

        Profit = GetUSDTProfit(FixedWithdrawFee, DynamicWithdrawForCurrencyBudgetFee, diffPercent);
    }

    private decimal GetUSDTProfit(decimal staticWithdrawFee, decimal percentWithdrawFee, decimal diffPercent)
    {
        return Budget.PercentOf(diffPercent) - staticWithdrawFee - percentWithdrawFee;
    }

    private decimal GetWithdrawStaticFee(decimal price, decimal fee)
    {
        return price * fee;
    }

    private decimal GetWithdrawDynamicFee(decimal price, decimal feePercent)
    {
        return Budget.PercentOf(feePercent);
    }
}