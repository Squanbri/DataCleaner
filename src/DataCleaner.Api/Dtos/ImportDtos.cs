using DataCleaner.Api.Entities;

namespace DataCleaner.Api.Dtos;

public sealed record ImportBatchDto(
    int Id,
    string FileName,
    ImportStatus Status,
    int TotalRows,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public sealed record ImportAcceptedDto(
    int Id,
    string FileName,
    ImportStatus Status,
    int TotalRows);

public sealed record DuplicateRecordDto(
    int Id,
    int RowNumber,
    string? ExternalId,
    string? RawFullName,
    string? RawPhone,
    string? RawEmail,
    string? LastName,
    string? FirstName,
    string? MiddleName,
    string? Phone,
    string? Email);

public sealed record DuplicateGroupDto(
    int Id,
    string MatchReason,
    IReadOnlyList<DuplicateRecordDto> Records);
