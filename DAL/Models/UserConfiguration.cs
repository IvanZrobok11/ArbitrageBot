using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class UserConfiguration
    {
        public UserConfiguration() { }
        public UserConfiguration(long telegramUserId, int budget, byte minChanceToBuy, byte minChangeToSell, int exceptedProfit)
        {
            TelegramUserId = telegramUserId;
            Budget = budget;
            MinChanceToBuy = minChanceToBuy;
            MinChangeToSell = minChangeToSell;
            ExceptedProfit = exceptedProfit;
        }

        [Key]
        public long TelegramUserId { get; set; }
        public int Budget { get; set; }
        public byte MinChanceToBuy { get; set; }
        public byte MinChangeToSell { get; set; }
        public int ExceptedProfit { get; set; }
    }
}
