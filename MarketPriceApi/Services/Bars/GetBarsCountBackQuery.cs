using MarketPriceApi.Models.DTOs;
using MarketPriceApi.Services.Auth;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace MarketPriceApi.Services.Bars
{
    public class GetBarsCountBackQuery
    {
        private readonly HttpClient _httpClient;
        private readonly FintaAuthService _authService;
        private readonly IConfiguration _configuration;

        public GetBarsCountBackQuery(HttpClient httpClient, FintaAuthService authService, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _authService = authService;
            _configuration = configuration;
        }

        public async Task<BarsResponse> GetBarsCountBackAsync(
            string instrumentId, 
            string provider, 
            int interval, 
            string periodicity, 
            int barsCount)
        {
            var token = await _authService.GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var baseUrl = _configuration["Finta:BaseUrl"];
            var url = $"{baseUrl}/api/bars/v1/bars/count-back?instrumentId={instrumentId}&provider={provider}&interval={interval}&periodicity={periodicity}&barsCount={barsCount}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get bars count back: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<BarsResponse>(jsonResponse);
            
            return result ?? new BarsResponse();
        }
    }
}
