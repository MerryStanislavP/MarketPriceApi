using MarketPriceApi.Services.Instruments;
using MarketPriceApi.Services.Bars;
using MarketPriceApi.Services.Assets;
using MarketPriceApi.Services.Prices;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MarketPriceApi.Services.Sync
{
    public class FintaSyncService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<FintaSyncService> _logger;

        public FintaSyncService(IMediator mediator, ILogger<FintaSyncService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<int> SyncInstrumentsAsync(string? provider = null, string? kind = null)
        {
            _logger.LogInformation("Starting instruments synchronization for Provider: {Provider}, Kind: {Kind}", provider, kind);

            var command = new SyncInstrumentsCommand
            {
                Provider = provider,
                Kind = kind
            };

            var result = await _mediator.Send(command);
            
            _logger.LogInformation("Instruments synchronization completed. Total synced: {TotalSynced}", result);
            
            return result;
        }

        public async Task SyncPricesForSymbolAsync(string symbol, string provider, string interval = "1m", int barsCount = 100)
        {
            _logger.LogInformation("Starting price synchronization for Symbol: {Symbol}, Provider: {Provider}, Interval: {Interval}", symbol, provider, interval);

            try
            {
                // Получаем исторические цены
                var query = new GetHistoricalPricesQuery
                {
                    Symbol = symbol,
                    Provider = provider,
                    Interval = interval,
                    StartDate = DateTime.UtcNow.AddDays(-1), // Последние 24 часа
                    EndDate = DateTime.UtcNow
                };

                var prices = await _mediator.Send(query);
                
                _logger.LogInformation("Price synchronization completed for {Symbol}. Retrieved {Count} prices", symbol, prices.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during price synchronization for {Symbol}", symbol);
                throw;
            }
        }

        public async Task SyncAllActiveAssetsAsync()
        {
            _logger.LogInformation("Starting synchronization for all active assets");

            try
            {
                // Получаем все активные активы
                var assetsQuery = new GetAssetsQuery
                {
                    IsActive = true
                };

                var assets = await _mediator.Send(assetsQuery);

                foreach (var asset in assets)
                {
                    try
                    {
                        await SyncPricesForSymbolAsync(asset.Symbol, asset.Provider);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing prices for asset {Symbol}", asset.Symbol);
                        // Продолжаем с другими активами
                    }
                }

                _logger.LogInformation("Synchronization completed for {Count} assets", assets.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk synchronization");
                throw;
            }
        }
    }
} 