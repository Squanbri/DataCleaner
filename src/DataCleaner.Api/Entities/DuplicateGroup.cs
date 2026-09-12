namespace DataCleaner.Api.Entities;

public class DuplicateGroup
{
    public int Id { get; set; }
    public int ImportBatchId { get; set; }
    public required string MatchReason { get; set; }

    public ImportBatch ImportBatch { get; set; } = null!;
    public List<CustomerRecord> Records { get; set; } = [];
}
