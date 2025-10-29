namespace ChatProxy.Domain.Filter
{
    public sealed class BlacklistOptions
    {
        public string[] Words { get; set; } = Array.Empty<string>();
    }
}
