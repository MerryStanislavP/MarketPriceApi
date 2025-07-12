using System.ComponentModel.DataAnnotations;

namespace MarketPriceApi.Models
{
    public class SyncLog
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Operation { get; set; } = string.Empty; // SyncInstruments, SyncPrices, etc.

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public bool IsSuccess { get; set; }

        public int RecordsProcessed { get; set; }

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        [MaxLength(50)]
        public string? Provider { get; set; }

        [MaxLength(20)]
        public string? Kind { get; set; }
        public Guid? AssetId { get; set; } 
        public Asset? Asset { get; set; }

        public Guid? InstrumentId { get; set; } 
        public string? Symbol { get; set; } 
    }
}