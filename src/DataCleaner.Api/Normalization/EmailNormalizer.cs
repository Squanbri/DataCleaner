using System.Net.Mail;

namespace DataCleaner.Api.Normalization;

public sealed class EmailNormalizer
{
    public EmailResult Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new EmailResult(null, IsValid: true);
        }

        var normalized = raw.Trim().ToLowerInvariant();

        if (!MailAddress.TryCreate(normalized, out var address))
        {
            return new EmailResult(null, IsValid: false);
        }

        var host = address.Host;
        if (string.IsNullOrEmpty(host) || !host.Contains('.'))
        {
            return new EmailResult(null, IsValid: false);
        }

        return new EmailResult(address.Address, IsValid: true);
    }
}
