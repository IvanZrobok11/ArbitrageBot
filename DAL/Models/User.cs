using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class User
    {
        public User() { }

        public User(long telegramUserId, string? telegramName, string? authPhrase = null)
        {
            TelegramUserId = telegramUserId;
            TelegramName = telegramName;
            AuthPhrase = authPhrase;
        }

        [Key]
        public long TelegramUserId { get; set; }
        public string? TelegramName { get; set; }
        public string? AuthPhrase { get; set; }

        public UserConfiguration? UserConfiguration { get; set; }
    }
}
