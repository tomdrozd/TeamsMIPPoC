using System.Text.RegularExpressions;

namespace TeamsMIPPoC.Api;

internal static class UrlExtractor
{
    private static readonly Regex FirstHttpsUrlRegex = new(
        @"https://\S+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string? ExtractFirstHttpsUrl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = FirstHttpsUrlRegex.Match(text);
        return match.Success ? match.Value.TrimEnd('.', ',', ';', ':', ')', ']', '}') : null;
    }
}
