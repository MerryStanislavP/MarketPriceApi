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

        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty; // oanda, alpaca, etc.

        [MaxLength(20)]
        public string Kind { get; set; } = string.Empty; // forex, stock, crypto

        [MaxLength(50)]
        public string? Exchange { get; set; } // NASDAQ, NYSE, etc.

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastSyncAt { get; set; }

        public ICollection<AssetPrice> Prices { get; set; } = new List<AssetPrice>();
    }
}
