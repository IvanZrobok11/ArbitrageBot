using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class BlackAsset
    {
        public BlackAsset() { }
        public BlackAsset(string name)
        {
            Name = name;
        }

        [Key]
        public long Id { get; set; }

        [Required, MinLength(1), MaxLength(15)]
        public string Name { get; set; }
    }
}
