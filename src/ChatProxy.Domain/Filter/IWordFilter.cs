namespace ChatProxy.Domain.Filter;
public interface IWordFilter
{
    bool ContainsBlacklistedWord(string text, out string? found);
}