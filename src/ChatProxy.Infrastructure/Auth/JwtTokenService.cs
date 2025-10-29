using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChatProxy.Domain.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ChatProxy.Infrastructure.Auth;

public sealed class JwtTokenService
{
    private readonly JwtOptions _opt;
    public JwtTokenService(IOptions<JwtOptions> options) => _opt = options.Value;

    public string Create(string username)
    {
        var key = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY")
                  ?? _opt.SigningKey
                  ?? throw new InvalidOperationException("JWT signing key not configured.");

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            notBefore: now,
            expires: now.AddMinutes(_opt.ExpiresMinutes),
            claims: new[] { new Claim(ClaimTypes.Name, username) },
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
