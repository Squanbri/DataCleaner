using DataCleaner.Api.Normalization;

namespace DataCleaner.Tests.Normalization;

public class PhoneNormalizerTests
{
    private readonly PhoneNormalizer _sut = new();

    [Theory]
    [InlineData("8 (915) 123-45-67", "+79151234567")]
    [InlineData("+79151234567", "+79151234567")]
    [InlineData("915 123 45 67", "+79151234567")]
    [InlineData("8-915-1234567", "+79151234567")]
    [InlineData("79151234567", "+79151234567")]
    public void Normalize_ValidFormats_ReturnsE164(string raw, string expected)
    {
        var result = _sut.Normalize(raw);

        Assert.True(result.IsValid);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_Empty_ReturnsNullWithoutError(string? raw)
    {
        var result = _sut.Normalize(raw);

        Assert.True(result.IsValid);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abc")]
    [InlineData("812345")]
    [InlineData("123456789012")]
    public void Normalize_Garbage_ReturnsInvalid(string raw)
    {
        var result = _sut.Normalize(raw);

        Assert.False(result.IsValid);
        Assert.Null(result.Value);
    }
}
