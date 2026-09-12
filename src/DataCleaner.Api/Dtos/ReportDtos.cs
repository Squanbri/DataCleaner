using DataCleaner.Api.Entities;

namespace DataCleaner.Api.Dtos;

public sealed record QualityReportDto(
    int BatchId,
    int TotalRecords,
    int CleanRecords,
    int RecordsWithIssues,
    IReadOnlyDictionary<string, int> IssueCounts,
    IReadOnlyDictionary<string, double> FieldFillRates,
    int DuplicateGroupCount,
    int RecordsInDuplicateGroups);

public sealed record CustomerRecordDto(
    int Id,
    int RowNumber,
    string? ExternalId,
    string? RawFullName,
    string? RawPhone,
    string? RawEmail,
    string? RawBirthDate,
    string? RawCity,
    string? LastName,
    string? FirstName,
    string? MiddleName,
    string? Phone,
    string? Email,
    DateOnly? BirthDate,
    string? City,
    QualityIssues Issues,
    int? DuplicateGroupId);

public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
