namespace BusinessLogic.Models;

public record UserConfigurationDTO(
    int Budget,
    byte MinChanceToBuy,
    byte MinChangeToSell,
    decimal ExceptedProfit,
    string? TickerFilter
);
