using Microsoft.AspNetCore.Mvc;
using MediatR;
using MarketPriceApi.Services.Assets;
using MarketPriceApi.Services.Prices;
using MarketPriceApi.Services.Sync;
using MarketPriceApi.Services.WebSocket;

namespace MarketPriceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketDataController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly FintaSyncService _syncService;
        private readonly FintaWebSocketService _webSocketService;

        public MarketDataController(
            IMediator mediator,
            FintaSyncService syncService,
            FintaWebSocketService webSocketService)
        {
            _mediator = mediator;
            _syncService = syncService;
            _webSocketService = webSocketService;
        }

        // GET: api/marketdata/assets
        [HttpGet("assets")]
        public async Task<IActionResult> GetAssets(
            [FromQuery] string? provider,
            [FromQuery] string? kind,
            [FromQuery] string? symbol,
            [FromQuery] bool? isActive)
        {
            var query = new GetAssetsQuery
            {
                Provider = provider,
                Kind = kind,
                Symbol = symbol,
                IsActive = isActive
            };

            var assets = await _mediator.Send(query);
            return Ok(assets);
        }

        // GET: api/marketdata/prices/current/{symbol}
        [HttpGet("prices/current/{symbol}")]
        public async Task<IActionResult> GetCurrentPrice(
            string symbol,
            [FromQuery] string provider,
            [FromQuery] string interval = "1m")
        {
            var query = new GetCurrentPriceQuery
            {
                Symbol = symbol,
                Provider = provider,
                Interval = interval
            };

            var price = await _mediator.Send(query);
            
            if (price == null)
                return NotFound($"Price not found for symbol {symbol}");

            return Ok(price);
        }

        // GET: api/marketdata/prices/historical/{symbol}
        [HttpGet("prices/historical/{symbol}")]
        public async Task<IActionResult> GetHistoricalPrices(
            string symbol,
            [FromQuery] string provider,
            [FromQuery] string interval = "1m",
            [FromQuery] DateTime startDate = default,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? limit = null)
        {
            if (startDate == default)
                startDate = DateTime.UtcNow.AddDays(-1);

            var query = new GetHistoricalPricesQuery
            {
                Symbol = symbol,
                Provider = provider,
                Interval = interval,
                StartDate = startDate,
                EndDate = endDate,
                Limit = limit
            };

            var prices = await _mediator.Send(query);
            return Ok(prices);
        }

        // POST: api/marketdata/prices
        [HttpPost("prices")]
        public async Task<IActionResult> SavePrice([FromBody] SavePriceCommand command)
        {
            var priceId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetCurrentPrice), new { symbol = command.Symbol }, new { Id = priceId });
        }

        // POST: api/marketdata/sync/instruments
        [HttpPost("sync/instruments")]
        public async Task<IActionResult> SyncInstruments(
            [FromQuery] string? provider,
            [FromQuery] string? kind)
        {
            var syncedCount = await _syncService.SyncInstrumentsAsync(provider, kind);
            return Ok(new { SyncedCount = syncedCount });
        }

        // POST: api/marketdata/sync/prices/{symbol}
        [HttpPost("sync/prices/{symbol}")]
        public async Task<IActionResult> SyncPrices(
            string symbol,
            [FromQuery] string provider,
            [FromQuery] string interval = "1m")
        {
            await _syncService.SyncPricesForSymbolAsync(symbol, provider, interval);
            return Ok(new { Message = $"Prices synced for {symbol}" });
        }

        // POST: api/marketdata/sync/all
        [HttpPost("sync/all")]
        public async Task<IActionResult> SyncAllAssets()
        {
            await _syncService.SyncAllActiveAssetsAsync();
            return Ok(new { Message = "All assets synchronized" });
        }

        // POST: api/marketdata/websocket/start
        [HttpPost("websocket/start")]
        public async Task<IActionResult> StartWebSocket()
        {
            await _webSocketService.StartAsync();
            return Ok(new { Message = "WebSocket service started" });
        }

        // POST: api/marketdata/websocket/stop
        [HttpPost("websocket/stop")]
        public async Task<IActionResult> StopWebSocket()
        {
            await _webSocketService.StopAsync();
            return Ok(new { Message = "WebSocket service stopped" });
        }

        // POST: api/marketdata/websocket/subscribe/{symbol}
        [HttpPost("websocket/subscribe/{symbol}")]
        public async Task<IActionResult> SubscribeToSymbol(
            string symbol,
            [FromQuery] string provider)
        {
            await _webSocketService.SubscribeToSymbolAsync(symbol, provider);
            return Ok(new { Message = $"Subscribed to {symbol}" });
        }
    }
} 