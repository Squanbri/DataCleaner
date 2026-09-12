namespace DataCleaner.Api.Entities;

public class ImportBatch
{
    public int Id { get; set; }
    public required string FileName { get; set; }
    public string? FilePath { get; set; }
    public ImportStatus Status { get; set; }
    public int TotalRows { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public List<CustomerRecord> Records { get; set; } = [];
    public List<DuplicateGroup> DuplicateGroups { get; set; } = [];
}
