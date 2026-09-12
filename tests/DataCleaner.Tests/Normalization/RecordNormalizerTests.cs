using DataCleaner.Api.Entities;
using DataCleaner.Api.Normalization;

namespace DataCleaner.Tests.Normalization;

public class RecordNormalizerTests
{
    private readonly RecordNormalizer _sut = new(
        new PhoneNormalizer(),
        new EmailNormalizer(),
        new NameNormalizer(),
        new BirthDateParser(),
        new CityNormalizer());

    [Fact]
    public void Normalize_SetsFlagsForBrokenContactAndName()
    {
        var row = new CsvRow
        {
            FullName = "",
            Phone = "123",
            Email = "bad",
            BirthDate = "31.02.1990",
            City = "г. казань",
        };

        var record = _sut.Normalize(row, importBatchId: 1, rowNumber: 7);

        Assert.Equal(1, record.ImportBatchId);
        Assert.Equal(7, record.RowNumber);
        Assert.Equal("г. казань", record.RawCity);
        Assert.Equal("Казань", record.City);
        Assert.True(record.Issues.HasFlag(QualityIssues.MissingName));
        Assert.True(record.Issues.HasFlag(QualityIssues.InvalidPhone));
        Assert.True(record.Issues.HasFlag(QualityIssues.InvalidEmail));
        Assert.True(record.Issues.HasFlag(QualityIssues.InvalidBirth));
        Assert.True(record.Issues.HasFlag(QualityIssues.MissingContact));
    }

    [Fact]
    public void Normalize_ValidRow_HasNoIssues()
    {
        var row = new CsvRow
        {
            ExternalId = "crm-1",
            FullName = "Иванов Иван Иванович",
            Phone = "8 (915) 123-45-67",
            Email = "Ivan@Mail.RU",
            BirthDate = "12.05.1990",
            City = "г. Москва",
        };

        var record = _sut.Normalize(row, importBatchId: 2, rowNumber: 1);

        Assert.Equal(QualityIssues.None, record.Issues);
        Assert.Equal("+79151234567", record.Phone);
        Assert.Equal("ivan@mail.ru", record.Email);
        Assert.Equal("Иванов", record.LastName);
        Assert.Equal(new DateOnly(1990, 5, 12), record.BirthDate);
    }
}
