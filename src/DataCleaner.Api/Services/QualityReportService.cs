using DataCleaner.Api.Data;
using DataCleaner.Api.Dtos;
using DataCleaner.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataCleaner.Api.Services;

public sealed class QualityReportService(AppDbContext db)
{
    public async Task<QualityReportDto?> GetReportAsync(int batchId, CancellationToken cancellationToken = default)
    {
        var exists = await db.ImportBatches.AsNoTracking()
            .AnyAsync(b => b.Id == batchId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var records = db.CustomerRecords.AsNoTracking().Where(r => r.ImportBatchId == batchId);

        var total = await records.CountAsync(cancellationToken);
        var clean = await records.CountAsync(r => r.Issues == QualityIssues.None, cancellationToken);
        var withIssues = total - clean;

        var issueCounts = new Dictionary<string, int>
        {
            [nameof(QualityIssues.MissingName)] = await records.CountAsync(
                r => (r.Issues & QualityIssues.MissingName) != 0, cancellationToken),
            [nameof(QualityIssues.InvalidPhone)] = await records.CountAsync(
                r => (r.Issues & QualityIssues.InvalidPhone) != 0, cancellationToken),
            [nameof(QualityIssues.InvalidEmail)] = await records.CountAsync(
                r => (r.Issues & QualityIssues.InvalidEmail) != 0, cancellationToken),
            [nameof(QualityIssues.InvalidBirth)] = await records.CountAsync(
                r => (r.Issues & QualityIssues.InvalidBirth) != 0, cancellationToken),
            [nameof(QualityIssues.MissingContact)] = await records.CountAsync(
                r => (r.Issues & QualityIssues.MissingContact) != 0, cancellationToken),
        };

        var fillRates = new Dictionary<string, double>();
        if (total == 0)
        {
            fillRates["LastName"] = 0;
            fillRates["FirstName"] = 0;
            fillRates["MiddleName"] = 0;
            fillRates["Phone"] = 0;
            fillRates["Email"] = 0;
            fillRates["BirthDate"] = 0;
            fillRates["City"] = 0;
        }
        else
        {
            fillRates["LastName"] = await records.CountAsync(r => r.LastName != null, cancellationToken) / (double)total;
            fillRates["FirstName"] = await records.CountAsync(r => r.FirstName != null, cancellationToken) / (double)total;
            fillRates["MiddleName"] = await records.CountAsync(r => r.MiddleName != null, cancellationToken) / (double)total;
            fillRates["Phone"] = await records.CountAsync(r => r.Phone != null, cancellationToken) / (double)total;
            fillRates["Email"] = await records.CountAsync(r => r.Email != null, cancellationToken) / (double)total;
            fillRates["BirthDate"] = await records.CountAsync(r => r.BirthDate != null, cancellationToken) / (double)total;
            fillRates["City"] = await records.CountAsync(r => r.City != null, cancellationToken) / (double)total;
        }

        var duplicateGroupCount = await db.DuplicateGroups.AsNoTracking()
            .CountAsync(g => g.ImportBatchId == batchId, cancellationToken);
        var recordsInDuplicateGroups = await records.CountAsync(r => r.DuplicateGroupId != null, cancellationToken);

        return new QualityReportDto(
            batchId,
            total,
            clean,
            withIssues,
            issueCounts,
            fillRates,
            duplicateGroupCount,
            recordsInDuplicateGroups);
    }

    public async Task<PagedResultDto<CustomerRecordDto>?> GetRecordsAsync(
        int batchId,
        QualityIssues? issue,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var exists = await db.ImportBatches.AsNoTracking()
            .AnyAsync(b => b.Id == batchId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var query = db.CustomerRecords.AsNoTracking()
            .Where(r => r.ImportBatchId == batchId);

        if (issue is { } flag and not QualityIssues.None)
        {
            query = query.Where(r => (r.Issues & flag) != 0);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(r => r.RowNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new CustomerRecordDto(
                r.Id,
                r.RowNumber,
                r.ExternalId,
                r.RawFullName,
                r.RawPhone,
                r.RawEmail,
                r.RawBirthDate,
                r.RawCity,
                r.LastName,
                r.FirstName,
                r.MiddleName,
                r.Phone,
                r.Email,
                r.BirthDate,
                r.City,
                r.Issues,
                r.DuplicateGroupId))
            .ToListAsync(cancellationToken);

        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        return new PagedResultDto<CustomerRecordDto>(items, page, pageSize, totalItems, totalPages);
    }
}
