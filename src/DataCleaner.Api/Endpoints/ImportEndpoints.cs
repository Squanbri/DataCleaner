using DataCleaner.Api.Dtos;
using DataCleaner.Api.Services;

namespace DataCleaner.Api.Endpoints;

public static class ImportEndpoints
{
    public static RouteGroupBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/imports").WithTags("Imports");

        group.MapPost("/", ImportAsync)
            .DisableAntiforgery()
            .WithName("ImportCustomers");

        group.MapGet("/{id:int}", GetImportAsync)
            .WithName("GetImport");

        group.MapGet("/{id:int}/duplicates", GetDuplicatesAsync)
            .WithName("GetImportDuplicates");

        return group;
    }

    private static async Task<IResult> ImportAsync(
        IFormFile? file,
        ImportService imports,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["CSV file is required."],
            });
        }

        await using var stream = file.OpenReadStream();
        var result = await imports.ImportAsync(stream, file.FileName, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetImportAsync(
        int id,
        ImportService imports,
        CancellationToken cancellationToken)
    {
        var batch = await imports.GetBatchAsync(id, cancellationToken);
        return batch is null ? TypedResults.NotFound() : TypedResults.Ok(batch);
    }

    private static async Task<IResult> GetDuplicatesAsync(
        int id,
        ImportService imports,
        DeduplicationService deduplication,
        CancellationToken cancellationToken)
    {
        var batch = await imports.GetBatchAsync(id, cancellationToken);
        if (batch is null)
        {
            return TypedResults.NotFound();
        }

        var groups = await deduplication.GetDuplicateGroupsAsync(id, cancellationToken);
        return TypedResults.Ok(groups);
    }
}
