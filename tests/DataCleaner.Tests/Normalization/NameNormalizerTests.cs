using DataCleaner.Api.Normalization;

namespace DataCleaner.Tests.Normalization;

public class NameNormalizerTests
{
    private readonly NameNormalizer _sut = new();

    [Theory]
    [InlineData("ИВАНОВ ИВАН ИВАНОВИЧ", "Иванов", "Иван", "Иванович")]
    [InlineData("  иванов   иван  иванович ", "Иванов", "Иван", "Иванович")]
    [InlineData("Петров-Водкин Кузьма Сергеевич", "Петров-Водкин", "Кузьма", "Сергеевич")]
    [InlineData("сидоров петр", "Сидоров", "Петр", null)]
    public void Normalize_FormatsNameParts(
        string raw,
        string lastName,
        string firstName,
        string? middleName)
    {
        var result = _sut.Normalize(raw);

        Assert.True(result.IsValid);
        Assert.Equal(lastName, result.LastName);
        Assert.Equal(firstName, result.FirstName);
        Assert.Equal(middleName, result.MiddleName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_Empty_IsInvalid(string? raw)
    {
        var result = _sut.Normalize(raw);

        Assert.False(result.IsValid);
        Assert.Null(result.LastName);
    }
}
