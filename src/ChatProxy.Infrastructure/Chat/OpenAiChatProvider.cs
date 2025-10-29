using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ChatProxy.Domain.Chat;
using Microsoft.Extensions.Options;

namespace ChatProxy.Infrastructure.Chat
{
    public sealed class OpenAiChatProvider(HttpClient http, IOptions<OpenAiSettings> opt) : IChatProvider
    {
        private readonly HttpClient _http = http;
        private readonly OpenAiSettings _cfg = opt.Value;
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct)
        {
            var apiKey = _cfg.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OpenAI API key não configurada. Defina em OpenAi:ApiKey ou OPENAI_API_KEY.");

            _http.DefaultRequestHeaders.Clear();

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(_cfg.Project) && apiKey.StartsWith("sk-proj-", StringComparison.OrdinalIgnoreCase))
            {
                _http.DefaultRequestHeaders.Add("OpenAI-Project", _cfg.Project);
            }

            var body = new
            {
                model = string.IsNullOrWhiteSpace(_cfg.Model) ? "gpt-4o-mini" : _cfg.Model,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = string.IsNullOrWhiteSpace(request.System)
                            ? "You are a helpful assistant."
                            : request.System
                    },
                    new { role = "user", content = request.Prompt }
                },
                temperature = request.Temperature ?? 0.7
            };

            var url = string.IsNullOrWhiteSpace(_cfg.BaseUrl) ? "https://api.openai.com/v1" : _cfg.BaseUrl;
            var endpoint = $"{url.TrimEnd('/')}/chat/completions";

            var content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");
            var started = DateTime.UtcNow;
            using var resp = await _http.PostAsync(endpoint, content, ct);
            var elapsed = (DateTime.UtcNow - started).TotalMilliseconds.ToString("0");

            if (!resp.IsSuccessStatusCode)
            {
                var errorPayload = await resp.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"OpenAI HTTP {(int)resp.StatusCode} {resp.StatusCode}. Body: {errorPayload}");
            }

            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            var model = doc.RootElement.TryGetProperty("model", out var m)
                ? (m.GetString() ?? "unknown")
                : "unknown";

            return new ChatResponse(text, model, elapsed);
        }
    }
}
