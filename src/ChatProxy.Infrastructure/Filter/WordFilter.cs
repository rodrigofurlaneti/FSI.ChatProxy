using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ChatProxy.Domain.Filter;
using Microsoft.Extensions.Options;
using System.Threading;

namespace ChatProxy.Infrastructure.Filter;

public sealed class WordFilter : IWordFilter
{
    private volatile HashSet<string> _black;

    public WordFilter(IOptionsMonitor<BlacklistOptions> monitor)
    {
        _black = BuildSet(monitor.CurrentValue);
        monitor.OnChange(o => Volatile.Write(ref _black, BuildSet(o)));
    }

    public bool ContainsBlacklistedWord(string text, out string? found)
    {
        found = null;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var norm = Normalize(text);
        var tokens = Regex.Matches(norm, @"\b[\p{L}\p{Nd}]+\b", RegexOptions.CultureInvariant)
                          .Select(m => m.Value);

        var local = _black; // snapshot thread-safe
        foreach (var t in tokens)
        {
            if (local.Contains(t)) { found = t; return true; }
        }
        return false;
    }

    private static HashSet<string> BuildSet(BlacklistOptions opt) =>
        opt.Words?.Select(Normalize)
                 .Where(w => !string.IsNullOrWhiteSpace(w))
                 .ToHashSet(StringComparer.OrdinalIgnoreCase)
        ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static string Normalize(string s)
    {
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in formD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
