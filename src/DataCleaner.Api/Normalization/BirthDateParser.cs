using System.Globalization;

namespace DataCleaner.Api.Normalization;

public sealed class BirthDateParser
{
    private static readonly string[] Formats =
    [
        "dd.MM.yyyy",
        "yyyy-MM-dd",
        "dd/MM/yyyy",
        "d.M.yyyy",
    ];

    private static readonly DateOnly MinDate = new(1900, 1, 1);

    public BirthDateResult Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new BirthDateResult(null, IsValid: true);
        }

        if (!DateOnly.TryParseExact(
                raw.Trim(),
                Formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return new BirthDateResult(null, IsValid: false);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (date > today || date < MinDate)
        {
            return new BirthDateResult(null, IsValid: false);
        }

        return new BirthDateResult(date, IsValid: true);
    }
}
