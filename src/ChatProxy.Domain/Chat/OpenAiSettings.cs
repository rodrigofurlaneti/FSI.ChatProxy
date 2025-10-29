namespace ChatProxy.Domain.Chat
{
    public sealed class OpenAiSettings
    {
        public string? ApiKey { get; set; }
        public string? Project { get; set; }
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string Model { get; set; } = "gpt-4o-mini";
    }
}
