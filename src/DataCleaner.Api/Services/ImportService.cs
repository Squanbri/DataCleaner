using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataCleaner.Api.Data;
using DataCleaner.Api.Dtos;
using DataCleaner.Api.Entities;
using DataCleaner.Api.Messaging;
using DataCleaner.Api.Normalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DataCleaner.Api.Services;

public sealed class ImportService(
    AppDbContext db,
    RecordNormalizer recordNormalizer,
    DeduplicationService deduplication,
    ImportPublisher publisher,
    IOptions<StorageOptions> storageOptions,
    ILogger<ImportService> logger)
{
    private const int BatchSize = 1000;

    public async Task<ImportAcceptedDto> AcceptAsync(
        Stream file,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var uploadsRoot = Path.GetFullPath(storageOptions.Value.UploadsPath);
        Directory.CreateDirectory(uploadsRoot);

        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "upload.csv";
        }

        var storedName = $"{Guid.NewGuid():N}_{safeName}";
        var filePath = Path.Combine(uploadsRoot, storedName);

        await using (var output = File.Create(filePath))
        {
            await file.CopyToAsync(output, cancellationToken);
        }

        var batch = new ImportBatch
        {
            FileName = safeName,
            FilePath = filePath,
            Status = ImportStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await publisher.PublishAsync(batch.Id, cancellationToken);
        }
        catch
        {
            batch.Status = ImportStatus.Failed;
            batch.ErrorMessage = "Failed to enqueue import for background processing.";
            batch.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }

        logger.LogInformation("Import {BatchId} accepted and queued ({FileName})", batch.Id, batch.FileName);
        return new ImportAcceptedDto(batch.Id, batch.FileName, batch.Status, batch.TotalRows);
    }

    public async Task ProcessAsync(int batchId, CancellationToken cancellationToken = default)
    {
        var batch = await db.ImportBatches.FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch is null)
        {
            logger.LogWarning("Import batch {BatchId} not found", batchId);
            return;
        }

        // Idempotency: message may be redelivered after a crash between work and ack.
        if (batch.Status == ImportStatus.Completed)
        {
            logger.LogInformation("Import {BatchId} already completed; skipping", batchId);
            return;
        }

        if (string.IsNullOrWhiteSpace(batch.FilePath) || !File.Exists(batch.FilePath))
        {
            batch.Status = ImportStatus.Failed;
            batch.ErrorMessage = "Uploaded file is missing on disk.";
            batch.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        // Redelivery / crash recovery: drop partial rows from a previous attempt.
        await db.CustomerRecords
            .Where(r => r.ImportBatchId == batchId)
            .ExecuteDeleteAsync(cancellationToken);
        await db.DuplicateGroups
            .Where(g => g.ImportBatchId == batchId)
            .ExecuteDeleteAsync(cancellationToken);

        batch.Status = ImportStatus.Processing;
        batch.ErrorMessage = null;
        batch.TotalRows = 0;
        batch.CompletedAt = null;
        await db.SaveChangesAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.None,
                BadDataFound = null,
                MissingFieldFound = null,
            };

            await using var stream = File.OpenRead(batch.FilePath);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, config);

            var buffer = new List<CustomerRecord>(BatchSize);
            var rowNumber = 0;
            var totalRows = 0;

            await foreach (var row in csv.GetRecordsAsync<CsvRow>(cancellationToken))
            {
                rowNumber++;
                buffer.Add(recordNormalizer.Normalize(row, batch.Id, rowNumber));

                if (buffer.Count < BatchSize)
                {
                    continue;
                }

                await SaveChunkAsync(buffer, cancellationToken);
                totalRows += buffer.Count;
                buffer.Clear();
            }

            if (buffer.Count > 0)
            {
                await SaveChunkAsync(buffer, cancellationToken);
                totalRows += buffer.Count;
                buffer.Clear();
            }

            await deduplication.FindDuplicatesAsync(batch.Id, cancellationToken);

            var completed = await db.ImportBatches.FirstAsync(b => b.Id == batch.Id, cancellationToken);
            completed.TotalRows = totalRows;
            completed.Status = ImportStatus.Completed;
            completed.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Import {BatchId} completed: {TotalRows} rows in {ElapsedMs} ms",
                completed.Id,
                totalRows,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            var failed = await db.ImportBatches.FirstAsync(b => b.Id == batchId, cancellationToken);
            failed.Status = ImportStatus.Failed;
            failed.ErrorMessage = ex.Message;
            failed.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ImportBatchDto?> GetBatchAsync(int id, CancellationToken cancellationToken = default)
    {
        return await db.ImportBatches
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new ImportBatchDto(
                b.Id,
                b.FileName,
                b.Status,
                b.TotalRows,
                b.ErrorMessage,
                b.CreatedAt,
                b.CompletedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImportBatchDto>> GetBatchesAsync(CancellationToken cancellationToken = default)
    {
        return await db.ImportBatches
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new ImportBatchDto(
                b.Id,
                b.FileName,
                b.Status,
                b.TotalRows,
                b.ErrorMessage,
                b.CreatedAt,
                b.CompletedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task SaveChunkAsync(List<CustomerRecord> buffer, CancellationToken cancellationToken)
    {
        db.CustomerRecords.AddRange(buffer);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }
}
