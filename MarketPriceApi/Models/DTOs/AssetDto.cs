namespace MarketPriceApi.Models.DTOs
{
    public class AssetDto
    {
        public Guid Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string? Exchange { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSyncAt { get; set; }
    }
} 