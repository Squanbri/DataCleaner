using DataCleaner.Api.Normalization;

namespace DataCleaner.Tests.Normalization;

public class BirthDateParserTests
{
    private readonly BirthDateParser _sut = new();

    [Theory]
    [InlineData("12.05.1990", 1990, 5, 12)]
    [InlineData("1990-05-12", 1990, 5, 12)]
    [InlineData("12/05/1990", 1990, 5, 12)]
    [InlineData("1.5.1990", 1990, 5, 1)]
    public void Parse_SupportedFormats_ReturnsDate(string raw, int year, int month, int day)
    {
        var result = _sut.Parse(raw);

        Assert.True(result.IsValid);
        Assert.Equal(new DateOnly(year, month, day), result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Parse_Empty_ReturnsNullWithoutError(string? raw)
    {
        var result = _sut.Parse(raw);

        Assert.True(result.IsValid);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData("31.02.1990")]
    [InlineData("1899-01-01")]
    [InlineData("01.01.2099")]
    [InlineData("not-a-date")]
    public void Parse_InvalidOrOutOfRange_ReturnsError(string raw)
    {
        var result = _sut.Parse(raw);

        Assert.False(result.IsValid);
        Assert.Null(result.Value);
    }
}
