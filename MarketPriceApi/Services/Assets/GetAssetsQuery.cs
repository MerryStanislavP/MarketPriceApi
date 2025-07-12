using MediatR;
using MarketPriceApi.Models;
using MarketPriceApi.Models.DTOs;
using MarketPriceApi.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace MarketPriceApi.Services.Assets
{
    public class GetAssetsQuery : IRequest<List<AssetDto>>
    {
        public string? Provider { get; set; }
        public string? Kind { get; set; }
        public string? Symbol { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetAssetsQueryHandler : IRequestHandler<GetAssetsQuery, List<AssetDto>>
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;

        public GetAssetsQueryHandler(AppDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<List<AssetDto>> Handle(GetAssetsQuery request, CancellationToken cancellationToken)
        {
            // Создаем ключ кеша на основе параметров запроса
            var cacheKey = $"assets:{request.Provider ?? "all"}:{request.Kind ?? "all"}:{request.Symbol ?? "all"}:{request.IsActive?.ToString() ?? "all"}";
            
            // Пытаемся получить из кеша
            var cachedResult = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonSerializer.Deserialize<List<AssetDto>>(cachedResult) ?? new List<AssetDto>();
            }

            // Если нет в кеше, получаем из БД
            var query = _context.Assets.AsQueryable();

            if (!string.IsNullOrEmpty(request.Provider))
                query = query.Where(a => a.Provider == request.Provider);

            if (!string.IsNullOrEmpty(request.Kind))
                query = query.Where(a => a.Kind == request.Kind);

            if (!string.IsNullOrEmpty(request.Symbol))
                query = query.Where(a => a.Symbol.Contains(request.Symbol));

            if (request.IsActive.HasValue)
                query = query.Where(a => a.IsActive == request.IsActive.Value);

            var assets = await query
                .OrderBy(a => a.Symbol)
                .ToListAsync(cancellationToken);

            var result = assets.Select(a => new AssetDto
            {
                Id = a.Id,
                Symbol = a.Symbol,
                Name = a.Name,
                Provider = a.Provider,
                Kind = a.Kind,
                Exchange = a.Exchange,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                LastSyncAt = a.LastSyncAt
            }).ToList();

            // Кешируем результат на 5 минут
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), 
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }, 
                cancellationToken);

            return result;
        }
    }
} 