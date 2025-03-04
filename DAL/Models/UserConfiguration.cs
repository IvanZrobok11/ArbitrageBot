using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{

    public class UserConfiguration
    {
        public UserConfiguration() { }

        public UserConfiguration(long telegramUserId, int budget, byte minChanceToBuy, byte minChangeToSell, decimal exceptedProfit)
        {
            TelegramUserId = telegramUserId;
            Budget = budget;
            MinChanceToBuy = minChanceToBuy;
            MinChangeToSell = minChangeToSell;
            ExceptedProfit = exceptedProfit;
        }

        [Key, ForeignKey(nameof(User))]
        public long TelegramUserId { get; set; }
        public int Budget { get; set; }
        public byte MinChanceToBuy { get; set; }
        public byte MinChangeToSell { get; set; }
        public decimal ExceptedProfit { get; set; }

        public User User { get; set; }
    }
}
