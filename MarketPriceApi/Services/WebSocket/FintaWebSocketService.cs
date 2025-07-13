using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MarketPriceApi.Services.Prices;
using MarketPriceApi.Services.Auth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MarketPriceApi.Services.WebSocket
{
    public class FintaWebSocketService
    {
        private readonly string _baseWebSocketUrl;
        private readonly IMediator _mediator;
        private readonly ILogger<FintaWebSocketService> _logger;
        private readonly FintaAuthService _authService;
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isConnected = false;
        private readonly List<string> _defaultSymbols = new()
        {
            "EUR/USD", "GBP/USD", "USD/JPY", "USD/CHF", "AUD/USD", "USD/CAD", "NZD/USD"
        };

        public FintaWebSocketService(string webSocketUrl, IMediator mediator, ILogger<FintaWebSocketService> logger, FintaAuthService authService)
        {
            _baseWebSocketUrl = webSocketUrl;
            _mediator = mediator;
            _logger = logger;
            _authService = authService;
        }

        public async Task StartAsync()
        {
            if (_isConnected)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();

            try
            {
                // Получаем токен доступа
                var token = await _authService.GetAccessTokenAsync();
                var webSocketUrl = $"{_baseWebSocketUrl}/api/streaming/ws/v1/realtime?token={token}";
                
                _logger.LogInformation("Attempting to connect to WebSocket with token: {TokenPrefix}...", token.Substring(0, Math.Min(20, token.Length)));
                
                await _webSocket.ConnectAsync(new Uri(webSocketUrl), _cancellationTokenSource.Token);
                _isConnected = true;

                _logger.LogInformation("WebSocket connected to {WebSocketUrl}", webSocketUrl);

                // Автоматическая подписка на популярные символы
                await SubscribeToDefaultSymbolsAsync();

                // Запускаем обработку сообщений
                _ = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to WebSocket");
                _isConnected = false;
            }
        }

        public async Task StopAsync()
        {
            if (!_isConnected || _webSocket == null)
                return;

            _cancellationTokenSource?.Cancel();

            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopping service", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing WebSocket connection");
            }
            finally
            {
                _webSocket.Dispose();
                _webSocket = null;
                _isConnected = false;
            }
        }

        public async Task SubscribeToSymbolAsync(string symbol, string provider)
        {
            if (!_isConnected || _webSocket == null)
            {
                _logger.LogWarning("WebSocket is not connected. Cannot subscribe to {Symbol}", symbol);
                return;
            }

            var subscribeMessage = new
            {
                action = "subscribe",
                symbol = symbol,
                provider = provider
            };

            var json = JsonSerializer.Serialize(subscribeMessage);
            var buffer = Encoding.UTF8.GetBytes(json);

            try
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationTokenSource?.Token ?? CancellationToken.None);
                _logger.LogInformation("Subscribed to symbol {Symbol} from provider {Provider}", symbol, provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to symbol {Symbol}", symbol);
            }
        }

        private async Task SubscribeToDefaultSymbolsAsync()
        {
            foreach (var symbol in _defaultSymbols)
            {
                try
                {
                    await SubscribeToSymbolAsync(symbol, "oanda");
                    await Task.Delay(100); // Небольшая задержка между подписками
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to subscribe to default symbol {Symbol}", symbol);
                }
            }
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await ProcessMessageAsync(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket connection closed by server");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving WebSocket message");
                    break;
                }
            }

            _isConnected = false;
            
            // Попытка переподключения через 30 секунд
            if (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Attempting to reconnect WebSocket in 30 seconds...");
                await Task.Delay(30000, cancellationToken);
                await StartAsync();
            }
        }

        private async Task ProcessMessageAsync(string message)
        {
            try
            {
                _logger.LogDebug("Received WebSocket message: {Message}", message);
                
                // Парсим сообщение от WebSocket
                var priceData = JsonSerializer.Deserialize<WebSocketPriceData>(message);
                
                if (priceData != null)
                {
                    // Сохраняем цену в БД
                    var command = new SavePriceCommand
                    {
                        Symbol = priceData.Symbol,
                        Provider = priceData.Provider,
                        Open = priceData.Open,
                        High = priceData.High,
                        Low = priceData.Low,
                        Close = priceData.Close,
                        Volume = priceData.Volume,
                        Interval = "1m", // WebSocket обычно передает минутные данные
                        Timestamp = priceData.Timestamp
                    };

                    await _mediator.Send(command);
                    _logger.LogDebug("Saved price for {Symbol}: {Price}", priceData.Symbol, priceData.Close);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WebSocket message: {Message}", message);
            }
        }

        public bool IsConnected => _isConnected;

        private class WebSocketPriceData
        {
            public string Symbol { get; set; } = string.Empty;
            public string Provider { get; set; } = string.Empty;
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
            public decimal Volume { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
} 