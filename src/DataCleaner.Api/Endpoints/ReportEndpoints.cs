using DataCleaner.Api.Dtos;
using DataCleaner.Api.Entities;
using DataCleaner.Api.Services;

namespace DataCleaner.Api.Endpoints;

public static class ReportEndpoints
{
    public static RouteGroupBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/imports").WithTags("Reports");

        group.MapGet("/{id:int}/report", GetReportAsync)
            .WithName("GetImportQualityReport");

        group.MapGet("/{id:int}/records", GetRecordsAsync)
            .WithName("GetImportRecords");

        return group;
    }

    private static async Task<IResult> GetReportAsync(
        int id,
        QualityReportService reports,
        CancellationToken cancellationToken)
    {
        var report = await reports.GetReportAsync(id, cancellationToken);
        return report is null ? TypedResults.NotFound() : TypedResults.Ok(report);
    }

    private static async Task<IResult> GetRecordsAsync(
        int id,
        QualityIssues? issue,
        int page,
        int pageSize,
        QualityReportService reports,
        CancellationToken cancellationToken)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 50 : pageSize;

        var result = await reports.GetRecordsAsync(id, issue, page, pageSize, cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
