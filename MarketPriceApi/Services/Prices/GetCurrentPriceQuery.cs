using MediatR;
using MarketPriceApi.Models;
using MarketPriceApi.Models.DTOs;
using MarketPriceApi.Persistence;
using MarketPriceApi.Services.Bars;
using MarketPriceApi.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace MarketPriceApi.Services.Prices
{
    public class GetCurrentPriceQuery : IRequest<PriceDto?>
    {
        public string Symbol { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Interval { get; set; } = "1m";
    }

    public class GetCurrentPriceQueryHandler : IRequestHandler<GetCurrentPriceQuery, PriceDto?>
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly GetBarsCountBackQuery _barsQuery;

        public GetCurrentPriceQueryHandler(
            AppDbContext context, 
            IDistributedCache cache,
            GetBarsCountBackQuery barsQuery)
        {
            _context = context;
            _cache = cache;
            _barsQuery = barsQuery;
        }

        public async Task<PriceDto?> Handle(GetCurrentPriceQuery request, CancellationToken cancellationToken)
        {
            // Создаем ключ кеша
            var cacheKey = $"current_price:{request.Symbol}:{request.Provider}:{request.Interval}";
            
            // Пытаемся получить из кеша
            var cachedResult = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonSerializer.Deserialize<PriceDto>(cachedResult);
            }

            // Получаем актив
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.Symbol == request.Symbol && a.Provider == request.Provider, cancellationToken);

            if (asset == null)
                return null;

            // Пытаемся получить последнюю цену из БД
            var latestPrice = await _context.AssetPrices
                .Where(p => p.AssetId == asset.Id && p.Interval == request.Interval)
                .OrderByDescending(p => p.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            // Если цена устарела (старше 5 минут для 1m интервала), получаем свежую
            if (latestPrice == null || DateTime.UtcNow - latestPrice.Timestamp > TimeSpan.FromMinutes(5))
            {
                try
                {
                    // Получаем свежую цену из Fintacharts API
                    var barsResponse = await _barsQuery.GetBarsCountBackAsync(
                        instrumentId: asset.Symbol,
                        provider: request.Provider,
                        interval: GetIntervalMinutes(request.Interval),
                        periodicity: request.Interval,
                        barsCount: 1);

                    if (barsResponse?.Bars != null && barsResponse.Bars.Any())
                    {
                        var latestBar = barsResponse.Bars.First();
                        
                        // Сохраняем новую цену в БД
                        var newPrice = new AssetPrice
                        {
                            AssetId = asset.Id,
                            Open = latestBar.Open,
                            High = latestBar.High,
                            Low = latestBar.Low,
                            Close = latestBar.Close,
                            Volume = latestBar.Volume,
                            Interval = request.Interval,
                            Provider = request.Provider,
                            Timestamp = latestBar.Timestamp
                        };

                        await _context.AssetPrices.AddAsync(newPrice, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);

                        latestPrice = newPrice;
                    }
                }
                catch (Exception)
                {
                    // Если не удалось получить свежую цену, используем последнюю из БД
                    if (latestPrice == null)
                        return null;
                }
            }

            if (latestPrice == null)
                return null;

            var result = new PriceDto
            {
                Id = latestPrice.Id,
                AssetId = latestPrice.AssetId,
                Symbol = asset.Symbol,
                Open = latestPrice.Open,
                High = latestPrice.High,
                Low = latestPrice.Low,
                Close = latestPrice.Close,
                Volume = latestPrice.Volume,
                Interval = latestPrice.Interval,
                Provider = latestPrice.Provider,
                Timestamp = latestPrice.Timestamp
            };

            // Кешируем результат на 1 минуту
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), 
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) }, 
                cancellationToken);

            return result;
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