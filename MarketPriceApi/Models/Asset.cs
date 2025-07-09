using System.ComponentModel.DataAnnotations;

namespace MarketPriceApi.Models
{
    public class Asset
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Symbol { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Name { get; set; }
        public ICollection<AssetPrice> Prices { get; set; } = new List<AssetPrice>();

    }
}
