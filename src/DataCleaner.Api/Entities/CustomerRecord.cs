namespace DataCleaner.Api.Entities;

public class CustomerRecord
{
    public int Id { get; set; }
    public int ImportBatchId { get; set; }
    public int RowNumber { get; set; }
    public string? ExternalId { get; set; }

    public string? RawFullName { get; set; }
    public string? RawPhone { get; set; }
    public string? RawEmail { get; set; }
    public string? RawBirthDate { get; set; }
    public string? RawCity { get; set; }

    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? City { get; set; }

    public QualityIssues Issues { get; set; }
    public int? DuplicateGroupId { get; set; }

    public ImportBatch ImportBatch { get; set; } = null!;
    public DuplicateGroup? DuplicateGroup { get; set; }
}
