using System.Globalization;
using System.Text.RegularExpressions;

namespace DataCleaner.Api.Normalization;

public sealed partial class CityNormalizer
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public CityResult Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new CityResult(null);
        }

        var value = raw.Trim();
        value = CityPrefixSuffixRegex().Replace(value, string.Empty).Trim();
        if (value.Length == 0)
        {
            return new CityResult(null);
        }

        value = Ru.TextInfo.ToTitleCase(value.ToLower(Ru));
        return new CityResult(value);
    }

    [GeneratedRegex(@"^\s*г\.\s*|\s*г\.\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CityPrefixSuffixRegex();
}
