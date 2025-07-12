using MarketPriceApi.Models.DTOs;
using MarketPriceApi.Services.Auth;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace MarketPriceApi.Services.Bars
{
    public class GetBarsTimeBackQuery
    {
        private readonly HttpClient _httpClient;
        private readonly FintaAuthService _authService;
        private readonly IConfiguration _configuration;

        public GetBarsTimeBackQuery(HttpClient httpClient, FintaAuthService authService, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _authService = authService;
            _configuration = configuration;
        }

        public async Task<BarsResponse> GetBarsTimeBackAsync(
            string instrumentId, 
            string provider, 
            int interval, 
            string periodicity, 
            TimeSpan timeBack)
        {
            var token = await _authService.GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var timeBackString = $"{timeBack.Days}.{timeBack.Hours:D2}:{timeBack.Minutes:D2}:{timeBack.Seconds:D2}";
            var baseUrl = _configuration["Finta:BaseUrl"];
            var url = $"{baseUrl}/api/data-consolidators/bars/v1/bars/time-back?instrumentId={instrumentId}&provider={provider}&interval={interval}&periodicity={periodicity}&timeBack={timeBackString}";
            
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get bars time back: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<BarsResponse>(jsonResponse);
            
            return result ?? new BarsResponse();
        }
    }
}
