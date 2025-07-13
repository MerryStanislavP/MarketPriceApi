using MarketPriceApi.Models.DTOs;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketPriceApi.Services.Auth
{
    public class FintaAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FintaAuthService> _logger;
        private TokenResponse? _currentToken;
        private DateTime _tokenExpiry;

        public FintaAuthService(HttpClient httpClient, IConfiguration configuration, ILogger<FintaAuthService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (_currentToken != null && DateTime.UtcNow < _tokenExpiry)
            {
                _logger.LogDebug("Using cached token");
                return _currentToken.AccessToken;
            }

            var tokenRequest = new TokenRequest
            {
                Username = _configuration["Finta:Username"],
                Password = _configuration["Finta:Password"]
            };

            _logger.LogInformation("Attempting to get token for user: {Username}", tokenRequest.Username);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", tokenRequest.GrantType),
                new KeyValuePair<string, string>("client_id", tokenRequest.ClientId),
                new KeyValuePair<string, string>("username", tokenRequest.Username),
                new KeyValuePair<string, string>("password", tokenRequest.Password)
            });

            var baseUrl = _configuration["Finta:BaseUrl"];
            var tokenUrl = $"{baseUrl}/identity/realms/fintatech/protocol/openid-connect/token";
            
            _logger.LogInformation("Sending token request to: {TokenUrl}", tokenUrl);
            _logger.LogInformation("Request parameters: grant_type={GrantType}, client_id={ClientId}, username={Username}", 
                tokenRequest.GrantType, tokenRequest.ClientId, tokenRequest.Username);
            
            var response = await _httpClient.PostAsync(tokenUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get token: {StatusCode}. Response: {ErrorContent}", response.StatusCode, errorContent);
                throw new Exception($"Failed to get token: {response.StatusCode}. Response: {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            _currentToken = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);
            
            if (_currentToken == null)
            {
                throw new Exception("Failed to deserialize token response");
            }

            // Логируем значения токена для отладки
            Console.WriteLine($"[DEBUG] access_token: {_currentToken.AccessToken.Substring(0, Math.Min(20, _currentToken.AccessToken.Length))}...");
            Console.WriteLine($"[DEBUG] expires_in: {_currentToken.ExpiresIn}");

            _tokenExpiry = DateTime.UtcNow.AddSeconds(_currentToken.ExpiresIn - 300);

            _logger.LogInformation("Successfully obtained access token. Expires in {ExpiresIn} seconds", _currentToken.ExpiresIn);

            return _currentToken.AccessToken;
        }
    }
}
