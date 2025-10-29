namespace ChatProxy.Domain.Chat
{
    public sealed record ChatResponse(string Content, string Model, string ProviderLatencyMs);
}
