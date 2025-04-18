namespace BusinessLogic.Models
{
    public class CryptoAPISettings
    {
        public const string SectionKey = "CryptoAPI";
        public required Dictionary<ExchangeMarketType, CryptoExchange.Net.Authentication.ApiCredentials> ExchangesCredentials { get; set; }
    }
}
