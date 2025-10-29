using ChatProxy.Domain.Auth;
using ChatProxy.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ChatProxy.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly JwtTokenService _jwt;

    public AuthController(JwtTokenService jwt) => _jwt = jwt;

    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Username == "admin" && request.Password == "123Mudar")
        {
            var token = _jwt.Create(request.Username);
            return Ok(new { access_token = token, token_type = "Bearer" });
        }

        return Unauthorized();
    }
}
