using ChatProxy.Domain.Filter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChatProxy.Api.Controllers;

[ApiController]
[Route("blacklist")]
[Authorize] 
public sealed class BlacklistController : ControllerBase
{
    private readonly IOptionsMonitor<BlacklistOptions> _options;

    public BlacklistController(IOptionsMonitor<BlacklistOptions> options) => _options = options;

    [HttpGet]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(_options.CurrentValue.Words);
}
