using DataCleaner.Api.Normalization;

namespace DataCleaner.Tests.Normalization;

public class EmailNormalizerTests
{
    private readonly EmailNormalizer _sut = new();

    [Theory]
    [InlineData("  User@Example.COM ", "user@example.com")]
    [InlineData("ivan@mail.ru", "ivan@mail.ru")]
    public void Normalize_ValidEmail_TrimsAndLowercases(string raw, string expected)
    {
        var result = _sut.Normalize(raw);

        Assert.True(result.IsValid);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Normalize_Empty_ReturnsNullWithoutError(string? raw)
    {
        var result = _sut.Normalize(raw);

        Assert.True(result.IsValid);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("user@localhost")]
    [InlineData("user@")]
    public void Normalize_Invalid_ReturnsError(string raw)
    {
        var result = _sut.Normalize(raw);

        Assert.False(result.IsValid);
        Assert.Null(result.Value);
    }
}
