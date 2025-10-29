using ChatProxy.Domain.Chat;
using ChatProxy.Infrastructure.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatProxy.Api.Controllers
{
    [ApiController]
    [Route("chat")]
    [Authorize]
    public sealed class ChatController : ControllerBase
    {
        private readonly IChatProvider _chat;

        public ChatController(IChatProvider chat) => _chat = chat;

        [HttpPost("ask")]
        [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest(new { error = "Prompt obrigatório." });

            var result = await _chat.CompleteAsync(request, ct);
            return Ok(result);
        }
    }
}


