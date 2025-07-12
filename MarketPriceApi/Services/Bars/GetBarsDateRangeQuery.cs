using MarketPriceApi.Models;
using MarketPriceApi.Services.Auth;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using MarketPriceApi.Models.DTOs;

namespace MarketPriceApi.Services.Bars
{
    public class GetBarsDateRangeQuery
    {
        private readonly HttpClient _httpClient;
        private readonly FintaAuthService _authService;
        private readonly IConfiguration _configuration;

        public GetBarsDateRangeQuery(HttpClient httpClient, FintaAuthService authService, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _authService = authService;
            _configuration = configuration;
        }

        public async Task<BarsResponse> GetBarsDateRangeAsync(
            string instrumentId, 
            string provider, 
            int interval, 
            string periodicity, 
            DateTime startDate, 
            DateTime? endDate = null)
        {
            var token = await _authService.GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var baseUrl = _configuration["Finta:BaseUrl"];
            var url = $"{baseUrl}/api/bars/v1/bars/date-range?instrumentId={instrumentId}&provider={provider}&interval={interval}&periodicity={periodicity}&startDate={startDate:yyyy-MM-dd}";
            
            if (endDate.HasValue)
            {
                url += $"&endDate={endDate.Value:yyyy-MM-dd}";
            }

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get bars date range: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<BarsResponse>(jsonResponse);
            
            return result ?? new BarsResponse();
        }
    }
}
