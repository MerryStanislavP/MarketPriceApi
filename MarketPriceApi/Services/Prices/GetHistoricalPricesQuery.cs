using MediatR;
using MarketPriceApi.Models;
using MarketPriceApi.Models.DTOs;
using MarketPriceApi.Persistence;
using MarketPriceApi.Services.Bars;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace MarketPriceApi.Services.Prices
{
    public class GetHistoricalPricesQuery : IRequest<List<PriceDto>>
    {
        public string Symbol { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Interval { get; set; } = "1m";
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Limit { get; set; }
    }

    public class GetHistoricalPricesQueryHandler : IRequestHandler<GetHistoricalPricesQuery, List<PriceDto>>
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly GetBarsDateRangeQuery _barsQuery;

        public GetHistoricalPricesQueryHandler(
            AppDbContext context, 
            IDistributedCache cache,
            GetBarsDateRangeQuery barsQuery)
        {
            _context = context;
            _cache = cache;
            _barsQuery = barsQuery;
        }

        public async Task<List<PriceDto>> Handle(GetHistoricalPricesQuery request, CancellationToken cancellationToken)
        {
            // Создаем ключ кеша
            var endDate = request.EndDate ?? DateTime.UtcNow;
            var cacheKey = $"historical_prices:{request.Symbol}:{request.Provider}:{request.Interval}:{request.StartDate:yyyyMMdd}:{endDate:yyyyMMdd}:{request.Limit}";
            
            // Пытаемся получить из кеша
            var cachedResult = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonSerializer.Deserialize<List<PriceDto>>(cachedResult) ?? new List<PriceDto>();
            }

            // Получаем актив
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.Symbol == request.Symbol && a.Provider == request.Provider, cancellationToken);

            if (asset == null)
                return new List<PriceDto>();

            // Пытаемся получить цены из БД
            var query = _context.AssetPrices
                .Where(p => p.AssetId == asset.Id && 
                           p.Interval == request.Interval &&
                           p.Timestamp >= request.StartDate &&
                           p.Timestamp <= endDate);

            if (request.Limit.HasValue)
                query = query.OrderByDescending(p => p.Timestamp).Take(request.Limit.Value);
            else
                query = query.OrderByDescending(p => p.Timestamp);

            var dbPrices = await query.ToListAsync(cancellationToken);

            // Если данных в БД недостаточно или они устарели, получаем из API
            if (!dbPrices.Any() || IsDataOutdated(dbPrices, request.StartDate, endDate))
            {
                try
                {
                    var barsResponse = await _barsQuery.GetBarsDateRangeAsync(
                        instrumentId: asset.Symbol,
                        provider: request.Provider,
                        interval: GetIntervalMinutes(request.Interval),
                        periodicity: request.Interval,
                        startDate: request.StartDate,
                        endDate: endDate);

                    if (barsResponse?.Bars != null && barsResponse.Bars.Any())
                    {
                        var newPrices = new List<AssetPrice>();
                        
                        foreach (var bar in barsResponse.Bars)
                        {
                            var existingPrice = await _context.AssetPrices
                                .FirstOrDefaultAsync(p => p.AssetId == asset.Id && 
                                                        p.Interval == request.Interval && 
                                                        p.Timestamp == bar.Timestamp, 
                                                        cancellationToken);

                            if (existingPrice == null)
                            {
                                var newPrice = new AssetPrice
                                {
                                    AssetId = asset.Id,
                                    Open = bar.Open,
                                    High = bar.High,
                                    Low = bar.Low,
                                    Close = bar.Close,
                                    Volume = bar.Volume,
                                    Interval = request.Interval,
                                    Provider = request.Provider,
                                    Timestamp = bar.Timestamp
                                };

                                newPrices.Add(newPrice);
                            }
                        }

                        if (newPrices.Any())
                        {
                            await _context.AssetPrices.AddRangeAsync(newPrices, cancellationToken);
                            await _context.SaveChangesAsync(cancellationToken);
                        }

                        // Обновляем список цен из БД
                        dbPrices = await query.ToListAsync(cancellationToken);
                    }
                }
                catch (Exception)
                {
                    // Если не удалось получить данные из API, используем то что есть в БД
                }
            }

            var result = dbPrices.Select(p => new PriceDto
            {
                Id = p.Id,
                AssetId = p.AssetId,
                Symbol = asset.Symbol,
                Open = p.Open,
                High = p.High,
                Low = p.Low,
                Close = p.Close,
                Volume = p.Volume,
                Interval = p.Interval,
                Provider = p.Provider,
                Timestamp = p.Timestamp
            }).ToList();

            // Кешируем результат на 10 минут для исторических данных
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), 
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) }, 
                cancellationToken);

            return result;
        }

        private static bool IsDataOutdated(List<AssetPrice> prices, DateTime startDate, DateTime endDate)
        {
            if (!prices.Any())
                return true;

            var expectedCount = (endDate - startDate).TotalMinutes;
            var actualCount = prices.Count;

            // Если данных меньше 80% от ожидаемого количества, считаем их устаревшими
            return actualCount < expectedCount * 0.8;
        }

        private static int GetIntervalMinutes(string interval)
        {
            return interval switch
            {
                "1m" => 1,
                "5m" => 5,
                "15m" => 15,
                "30m" => 30,
                "1h" => 60,
                "4h" => 240,
                "1d" => 1440,
                _ => 1
            };
        }
    }
} 