namespace MarketPriceApi.Models.DTOs
{
    public class CurrentPriceDto
    {
        public Guid AssetId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; } // Close price
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Volume { get; set; }
        public string Interval { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
} 