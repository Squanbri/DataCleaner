using System.Globalization;
using System.Text;

namespace DataCleaner.Api.Normalization;

public sealed class NameNormalizer
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public NameResult Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new NameResult(null, null, null, IsValid: false);
        }

        var collapsed = CollapseSpaces(raw);
        var parts = collapsed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return new NameResult(null, null, null, IsValid: false);
        }

        var titled = parts.Select(ToTitleCaseWord).ToArray();
        var lastName = titled[0];
        var firstName = titled.Length > 1 ? titled[1] : null;
        var middleName = titled.Length > 2 ? string.Join(' ', titled.Skip(2)) : null;

        return new NameResult(lastName, firstName, middleName, IsValid: true);
    }

    private static string CollapseSpaces(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasSpace = false;

        foreach (var ch in value.Trim())
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }

                continue;
            }

            builder.Append(ch);
            previousWasSpace = false;
        }

        return builder.ToString();
    }

    private static string ToTitleCaseWord(string word)
    {
        if (!word.Contains('-'))
        {
            return Ru.TextInfo.ToTitleCase(word.ToLower(Ru));
        }

        return string.Join('-', word.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => Ru.TextInfo.ToTitleCase(part.ToLower(Ru))));
    }
}
