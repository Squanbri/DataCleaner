namespace DataCleaner.Api.Entities;

[Flags]
public enum QualityIssues
{
    None = 0,
    MissingName = 1 << 0,
    InvalidPhone = 1 << 1,
    InvalidEmail = 1 << 2,
    InvalidBirth = 1 << 3,
    MissingContact = 1 << 4,
}
