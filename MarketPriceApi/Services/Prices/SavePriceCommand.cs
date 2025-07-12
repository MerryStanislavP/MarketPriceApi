using MediatR;
using MarketPriceApi.Models;
using MarketPriceApi.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace MarketPriceApi.Services.Prices
{
    public class SavePriceCommand : IRequest<Guid>
    {
        public string Symbol { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public string Interval { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class SavePriceCommandHandler : IRequestHandler<SavePriceCommand, Guid>
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;

        public SavePriceCommandHandler(AppDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<Guid> Handle(SavePriceCommand request, CancellationToken cancellationToken)
        {
            // Получаем или создаем актив
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.Symbol == request.Symbol && a.Provider == request.Provider, cancellationToken);

            if (asset == null)
            {
                asset = new Asset
                {
                    Symbol = request.Symbol,
                    Provider = request.Provider,
                    Kind = "unknown", // Будет обновлено при синхронизации
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastSyncAt = DateTime.UtcNow
                };

                await _context.Assets.AddAsync(asset, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Проверяем, существует ли уже цена с таким временем
            var existingPrice = await _context.AssetPrices
                .FirstOrDefaultAsync(p => p.AssetId == asset.Id && 
                                        p.Interval == request.Interval && 
                                        p.Timestamp == request.Timestamp, 
                                        cancellationToken);

            if (existingPrice != null)
            {
                // Обновляем существующую цену
                existingPrice.Open = request.Open;
                existingPrice.High = request.High;
                existingPrice.Low = request.Low;
                existingPrice.Close = request.Close;
                existingPrice.Volume = request.Volume;
                existingPrice.Provider = request.Provider;

                _context.AssetPrices.Update(existingPrice);
                await _context.SaveChangesAsync(cancellationToken);

                // Инвалидируем кеш
                await InvalidateCache(request.Symbol, request.Provider, request.Interval, cancellationToken);

                return existingPrice.Id;
            }

            // Создаем новую цену
            var newPrice = new AssetPrice
            {
                AssetId = asset.Id,
                Open = request.Open,
                High = request.High,
                Low = request.Low,
                Close = request.Close,
                Volume = request.Volume,
                Interval = request.Interval,
                Provider = request.Provider,
                Timestamp = request.Timestamp
            };

            await _context.AssetPrices.AddAsync(newPrice, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Инвалидируем кеш
            await InvalidateCache(request.Symbol, request.Provider, request.Interval, cancellationToken);

            return newPrice.Id;
        }

        private async Task InvalidateCache(string symbol, string provider, string interval, CancellationToken cancellationToken)
        {
            // Удаляем кеш текущей цены
            var currentPriceKey = $"current_price:{symbol}:{provider}:{interval}";
            await _cache.RemoveAsync(currentPriceKey, cancellationToken);

            // Удаляем кеш исторических цен (частично)
            var historicalPricePattern = $"historical_prices:{symbol}:{provider}:{interval}:*";
            // Note: Redis не поддерживает wildcard удаление, поэтому удаляем только основные ключи
            // В реальном приложении можно использовать Redis SCAN для поиска и удаления ключей по паттерну
        }
    }
} 