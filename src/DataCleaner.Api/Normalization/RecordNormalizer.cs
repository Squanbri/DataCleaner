using DataCleaner.Api.Entities;

namespace DataCleaner.Api.Normalization;

public sealed class RecordNormalizer(
    PhoneNormalizer phoneNormalizer,
    EmailNormalizer emailNormalizer,
    NameNormalizer nameNormalizer,
    BirthDateParser birthDateParser,
    CityNormalizer cityNormalizer)
{
    public CustomerRecord Normalize(CsvRow row, int importBatchId, int rowNumber)
    {
        var name = nameNormalizer.Normalize(row.FullName);
        var phone = phoneNormalizer.Normalize(row.Phone);
        var email = emailNormalizer.Normalize(row.Email);
        var birthDate = birthDateParser.Parse(row.BirthDate);
        var city = cityNormalizer.Normalize(row.City);

        var issues = QualityIssues.None;
        if (!name.IsValid)
        {
            issues |= QualityIssues.MissingName;
        }

        if (!phone.IsValid)
        {
            issues |= QualityIssues.InvalidPhone;
        }

        if (!email.IsValid)
        {
            issues |= QualityIssues.InvalidEmail;
        }

        if (!birthDate.IsValid)
        {
            issues |= QualityIssues.InvalidBirth;
        }

        if (phone.Value is null && email.Value is null)
        {
            issues |= QualityIssues.MissingContact;
        }

        return new CustomerRecord
        {
            ImportBatchId = importBatchId,
            RowNumber = rowNumber,
            ExternalId = string.IsNullOrWhiteSpace(row.ExternalId) ? null : row.ExternalId.Trim(),
            RawFullName = row.FullName,
            RawPhone = row.Phone,
            RawEmail = row.Email,
            RawBirthDate = row.BirthDate,
            RawCity = row.City,
            LastName = name.LastName,
            FirstName = name.FirstName,
            MiddleName = name.MiddleName,
            Phone = phone.Value,
            Email = email.Value,
            BirthDate = birthDate.Value,
            City = city.Value,
            Issues = issues,
        };
    }
}
