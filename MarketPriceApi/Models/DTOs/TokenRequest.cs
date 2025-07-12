namespace MarketPriceApi.Models.DTOs
{
    public class TokenRequest
    {
        public string GrantType { get; set; } = "password";
        public string ClientId { get; set; } = "finta-platform";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
} 