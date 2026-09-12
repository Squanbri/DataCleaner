using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataCleaner.Api.Data;
using DataCleaner.Api.Dtos;
using DataCleaner.Api.Entities;
using DataCleaner.Api.Normalization;
using Microsoft.EntityFrameworkCore;

namespace DataCleaner.Api.Services;

public sealed class ImportService(
    AppDbContext db,
    RecordNormalizer recordNormalizer,
    DeduplicationService deduplication,
    ILogger<ImportService> logger)
{
    private const int BatchSize = 1000;

    public async Task<ImportAcceptedDto> ImportAsync(
        Stream file,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var batch = new ImportBatch
        {
            FileName = fileName,
            Status = ImportStatus.Processing,
            CreatedAt = DateTime.UtcNow,
        };

        db.ImportBatches.Add(batch);
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

            using var reader = new StreamReader(file);
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

            // ChangeTracker was cleared during chunk inserts; dedupe loads records by batch id.
            await deduplication.FindDuplicatesAsync(batch.Id, cancellationToken);

            // ChangeTracker.Clear() detached the batch during chunk inserts.
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

            return new ImportAcceptedDto(completed.Id, completed.FileName, completed.Status, completed.TotalRows);
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            var failed = await db.ImportBatches.FirstAsync(b => b.Id == batch.Id, cancellationToken);
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
