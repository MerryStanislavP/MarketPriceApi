using System.Text.Json.Serialization;

namespace MarketPriceApi.Models.DTOs
{
    public class TokenRequest
    {
        public string GrantType { get; set; } = "password";
        public string ClientId { get; set; } = "app-cli";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
} 