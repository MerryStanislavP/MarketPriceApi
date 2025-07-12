using MarketPriceApi.Models.DTOs;
using MarketPriceApi.Services.Auth;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace MarketPriceApi.Services.Instruments
{
    public class GetExchangesQuery
    {
        private readonly HttpClient _httpClient;
        private readonly FintaAuthService _authService;
        private readonly IConfiguration _configuration;

        public GetExchangesQuery(HttpClient httpClient, FintaAuthService authService, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _authService = authService;
            _configuration = configuration;
        }

        public async Task<List<Exchange>> GetExchangesAsync(string? provider = null)
        {
            var token = await _authService.GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var baseUrl = _configuration["Finta:BaseUrl"];
            var url = $"{baseUrl}/api/instruments/v1/exchanges";
            if (!string.IsNullOrEmpty(provider))
            {
                url += $"?provider={provider}";
            }

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get exchanges: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<Exchange>>(jsonResponse);
            
            return result ?? new List<Exchange>();
        }
    }
}
