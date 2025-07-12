using MarketPriceApi.Models.DTOs;
using MarketPriceApi.Services.Auth;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace MarketPriceApi.Services.Instruments
{
    public class GetInstrumentsQuery
    {
        private readonly HttpClient _httpClient;
        private readonly FintaAuthService _authService;
        private readonly IConfiguration _configuration;

        public GetInstrumentsQuery(HttpClient httpClient, FintaAuthService authService, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _authService = authService;
            _configuration = configuration;
        }

        public async Task<InstrumentsResponse> GetInstrumentsAsync(string? provider = null, string? kind = null, string? symbol = null, int page = 1, int size = 10)
        {
            var token = await _authService.GetAccessTokenAsync();
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
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get instruments: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<InstrumentsResponse>(jsonResponse);
            
            return result ?? new InstrumentsResponse();
        }
    }
}
