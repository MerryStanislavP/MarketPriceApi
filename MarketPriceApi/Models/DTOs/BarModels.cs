namespace MarketPriceApi.Models.DTOs
{
    public class BarsResponse
    {
        public List<Bar> Bars { get; set; } = new List<Bar>();
        public string InstrumentId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public int Interval { get; set; }
        public string Periodicity { get; set; } = string.Empty;
    }

    public class Bar
    {
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 