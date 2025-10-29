namespace ChatProxy.Domain.Auth
{
    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public int ExpiresMinutes { get; set; } = 60;
        public string? SigningKey { get; set; } 
    }
}
