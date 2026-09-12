using CsvHelper.Configuration.Attributes;

namespace DataCleaner.Api.Normalization;

public sealed class CsvRow
{
    [Name("external_id")]
    public string? ExternalId { get; set; }

    [Name("full_name")]
    public string? FullName { get; set; }

    [Name("phone")]
    public string? Phone { get; set; }

    [Name("email")]
    public string? Email { get; set; }

    [Name("birth_date")]
    public string? BirthDate { get; set; }

    [Name("city")]
    public string? City { get; set; }
}
