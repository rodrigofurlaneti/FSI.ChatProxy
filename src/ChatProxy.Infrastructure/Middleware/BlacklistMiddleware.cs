using System.Text.Json;
using ChatProxy.Domain.Chat;
using ChatProxy.Domain.Filter;
using Microsoft.AspNetCore.Http;

namespace ChatProxy.Infrastructure.Middleware;

public sealed class BlacklistMiddleware
{
    private readonly RequestDelegate _next;
    public BlacklistMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, IWordFilter filter)
    {
        if (ctx.Request.Path.StartsWithSegments("/chat/ask", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(ctx.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            // Permite ler o body e reposicioná-lo
            ctx.Request.EnableBuffering();

            // Lê o body como ChatRequest
            var req = await JsonSerializer.DeserializeAsync<ChatRequest>(
                ctx.Request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (req is null || string.IsNullOrWhiteSpace(req.Prompt))
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                ctx.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(ctx.Response.Body, new { error = "Prompt obrigatório." });
                ctx.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
                return;
            }

            if (filter.ContainsBlacklistedWord(req.Prompt, out var found))
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                ctx.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(ctx.Response.Body, new { error = "Conteúdo não permitido.", word = found });
                ctx.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
                return;
            }

            ctx.Request.Body.Position = 0;
        }

        await _next(ctx); 
    }
}
