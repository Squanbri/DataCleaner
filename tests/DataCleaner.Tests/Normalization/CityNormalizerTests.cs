using DataCleaner.Api.Normalization;

namespace DataCleaner.Tests.Normalization;

public class CityNormalizerTests
{
    private readonly CityNormalizer _sut = new();

    [Theory]
    [InlineData("г. москва", "Москва")]
    [InlineData("МОСКВА г.", "Москва")]
    [InlineData("  санкт-петербург ", "Санкт-Петербург")]
    public void Normalize_StripsCityPrefixAndTitleCases(string raw, string expected)
    {
        var result = _sut.Normalize(raw);

        Assert.Equal(expected, result.Value);
    }
}
