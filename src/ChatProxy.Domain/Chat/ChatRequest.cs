namespace ChatProxy.Domain.Chat
{
    public sealed record ChatRequest(string Prompt, double? Temperature = null, string? System = null);

}
