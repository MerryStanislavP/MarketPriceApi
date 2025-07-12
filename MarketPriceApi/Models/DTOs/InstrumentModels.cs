namespace MarketPriceApi.Models.DTOs
{
    public class InstrumentsResponse
    {
        public List<Instrument> Instruments { get; set; } = new List<Instrument>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
    }

    public class Instrument
    {
        public string Id { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string? Exchange { get; set; }
        public bool IsActive { get; set; }
    }

    public class Provider
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class Exchange
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
    }
} 