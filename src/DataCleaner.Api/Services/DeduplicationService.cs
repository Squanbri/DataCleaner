using DataCleaner.Api.Data;
using DataCleaner.Api.Dtos;
using DataCleaner.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataCleaner.Api.Services;

public sealed class DeduplicationService(AppDbContext db)
{
    public async Task<int> FindDuplicatesAsync(int batchId, CancellationToken cancellationToken = default)
    {
        var records = await db.CustomerRecords
            .Where(r => r.ImportBatchId == batchId)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        if (records.Count == 0)
        {
            return 0;
        }

        var uf = new UnionFind(records.Count);
        var reasonParts = Enumerable.Range(0, records.Count)
            .Select(_ => new HashSet<string>(StringComparer.Ordinal))
            .ToArray();

        LinkByKey(records, uf, reasonParts, r => r.Phone, "phone");
        LinkByKey(records, uf, reasonParts, r => r.Email, "email");

        var groupsCreated = 0;

        foreach (var component in uf.GetComponents())
        {
            if (component.Count < 2)
            {
                continue;
            }

            var reasons = new HashSet<string>(StringComparer.Ordinal);
            foreach (var index in component)
            {
                reasons.UnionWith(reasonParts[index]);
            }

            var matchReason = FormatMatchReason(reasons);

            var group = new DuplicateGroup
            {
                ImportBatchId = batchId,
                MatchReason = matchReason,
            };

            foreach (var index in component)
            {
                records[index].DuplicateGroup = group;
            }

            db.DuplicateGroups.Add(group);
            groupsCreated++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return groupsCreated;
    }

    public async Task<IReadOnlyList<DuplicateGroupDto>> GetDuplicateGroupsAsync(
        int batchId,
        CancellationToken cancellationToken = default)
    {
        return await db.DuplicateGroups
            .AsNoTracking()
            .Where(g => g.ImportBatchId == batchId)
            .OrderBy(g => g.Id)
            .Select(g => new DuplicateGroupDto(
                g.Id,
                g.MatchReason,
                g.Records
                    .OrderBy(r => r.RowNumber)
                    .Select(r => new DuplicateRecordDto(
                        r.Id,
                        r.RowNumber,
                        r.ExternalId,
                        r.RawFullName,
                        r.RawPhone,
                        r.RawEmail,
                        r.LastName,
                        r.FirstName,
                        r.MiddleName,
                        r.Phone,
                        r.Email))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    private static string FormatMatchReason(HashSet<string> reasons)
    {
        if (reasons.Count == 0)
        {
            return "unknown";
        }

        string[] order = ["phone", "email"];
        return string.Join('+', order.Where(reasons.Contains));
    }

    private static void LinkByKey(
        IReadOnlyList<CustomerRecord> records,
        UnionFind uf,
        HashSet<string>[] reasonParts,
        Func<CustomerRecord, string?> keySelector,
        string reason)
    {
        var firstIndexByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < records.Count; i++)
        {
            var key = keySelector(records[i]);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (firstIndexByKey.TryGetValue(key, out var first))
            {
                uf.Union(first, i);
                reasonParts[first].Add(reason);
                reasonParts[i].Add(reason);
            }
            else
            {
                firstIndexByKey[key] = i;
            }
        }
    }
}
