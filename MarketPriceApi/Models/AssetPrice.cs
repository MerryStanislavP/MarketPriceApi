using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MarketPriceApi.Models
{
    public class AssetPrice
    {
        public Guid Id { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal Open { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal High { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal Low { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal Close { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal Volume { get; set; }

        [MaxLength(10)]
        public string Interval { get; set; } = string.Empty; // 1m, 5m, 1h, 1d

        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty; // источник данных

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public Guid AssetId { get; set; }
        public Asset Asset { get; set; } = null!;
    }
}
