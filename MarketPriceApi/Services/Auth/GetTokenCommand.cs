using MarketPriceApi.Models.DTOs;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MarketPriceApi.Services.Auth
{
    public class FintaAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private TokenResponse? _currentToken;
        private DateTime _tokenExpiry;

        public FintaAuthService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (_currentToken != null && DateTime.UtcNow < _tokenExpiry)
            {
                return _currentToken.AccessToken;
            }

            var tokenRequest = new TokenRequest
            {
                Username = _configuration["Finta:Username"],
                Password = _configuration["Finta:Password"]
            };

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", tokenRequest.GrantType),
                new KeyValuePair<string, string>("client_id", tokenRequest.ClientId),
                new KeyValuePair<string, string>("username", tokenRequest.Username),
                new KeyValuePair<string, string>("password", tokenRequest.Password)
            });

            var baseUrl = _configuration["Finta:BaseUrl"];
            var response = await _httpClient.PostAsync($"{baseUrl}/identity/realms/fintatech/protocol/openid-connect/token", content);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get token: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            _currentToken = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);
            
            if (_currentToken == null)
            {
                throw new Exception("Failed to deserialize token response");
            }

            _tokenExpiry = DateTime.UtcNow.AddSeconds(_currentToken.ExpiresIn - 300);

            return _currentToken.AccessToken;
        }
    }
}
