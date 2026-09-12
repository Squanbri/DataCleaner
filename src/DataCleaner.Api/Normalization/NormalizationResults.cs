namespace DataCleaner.Api.Normalization;

public sealed record PhoneResult(string? Value, bool IsValid);

public sealed record EmailResult(string? Value, bool IsValid);

public sealed record NameResult(
    string? LastName,
    string? FirstName,
    string? MiddleName,
    bool IsValid);

public sealed record BirthDateResult(DateOnly? Value, bool IsValid);

public sealed record CityResult(string? Value);
