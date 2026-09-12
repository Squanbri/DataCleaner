using System.Text;

namespace DataCleaner.Api.Normalization;

public sealed class PhoneNormalizer
{
    public PhoneResult Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new PhoneResult(null, IsValid: true);
        }

        var digits = new StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            if (char.IsDigit(ch))
            {
                digits.Append(ch);
            }
        }

        var number = digits.ToString();

        if (number.Length == 11 && number[0] == '8')
        {
            number = "7" + number[1..];
        }
        else if (number.Length == 10 && number[0] == '9')
        {
            number = "7" + number;
        }

        if (number.Length == 11 && number[0] == '7')
        {
            return new PhoneResult("+" + number, IsValid: true);
        }

        return new PhoneResult(null, IsValid: false);
    }
}
