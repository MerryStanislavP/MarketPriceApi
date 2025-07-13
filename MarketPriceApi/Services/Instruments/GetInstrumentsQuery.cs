using MarketPriceApi.Models.DTOs;
using MarketPriceApi.Services.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MarketPriceApi.Services.Instruments
{
    public class GetInstrumentsQuery
    {
        private readonly HttpClient _httpClient;
        private readonly FintaAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GetInstrumentsQuery> _logger;

        public GetInstrumentsQuery(HttpClient httpClient, FintaAuthService authService, IConfiguration configuration, ILogger<GetInstrumentsQuery> logger)
        {
            _httpClient = httpClient;
            _authService = authService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<InstrumentsResponse> GetInstrumentsAsync(string? provider = null, string? kind = null, string? symbol = null, int page = 1, int size = 10)
        {
            var token = await _authService.GetAccessTokenAsync();
            
            // Очищаем заголовки перед установкой нового токена
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(provider))
                queryParams.Add($"provider={provider}");
            if (!string.IsNullOrEmpty(kind))
                queryParams.Add($"kind={kind}");
            if (!string.IsNullOrEmpty(symbol))
                queryParams.Add($"symbol={symbol}");
            queryParams.Add($"page={page}");
            queryParams.Add($"size={size}");

            var baseUrl = _configuration["Finta:BaseUrl"];
            var url = $"{baseUrl}/api/instruments/v1/instruments?{string.Join("&", queryParams)}";

            // Логируем параметры для отладки
            _logger.LogInformation("[DEBUG] Запрос к Finta API: url={Url}", url);
            _logger.LogInformation("[DEBUG] Bearer token: {TokenPrefix}", token.Substring(0, Math.Min(20, token.Length)));
            _logger.LogInformation("[DEBUG] client_id (из TokenRequest): {ClientId}", new TokenRequest().ClientId);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("[DEBUG] Ошибка при запросе к Finta API: {StatusCode}, {ErrorContent}", response.StatusCode, errorContent);
                throw new Exception($"Failed to get instruments: {response.StatusCode}. Response: {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<InstrumentsResponse>(jsonResponse);
            
            return result ?? new InstrumentsResponse();
        }
    }
}
