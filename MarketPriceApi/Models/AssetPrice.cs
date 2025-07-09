using System.ComponentModel.DataAnnotations.Schema;

namespace MarketPriceApi.Models
{
    public class AssetPrice
    {
        public Guid Id { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal Price { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;


        public Guid AssetId { get; set; }
        public Asset Asset { get; set; }
    }
}
