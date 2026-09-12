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
