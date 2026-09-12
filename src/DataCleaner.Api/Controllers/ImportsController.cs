using DataCleaner.Api.Dtos;
using DataCleaner.Api.Entities;
using DataCleaner.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataCleaner.Api.Controllers;

public class ImportsController(
    ImportService imports,
    QualityReportService reports,
    DeduplicationService deduplication) : Controller
{
    [HttpGet("/")]
    [HttpGet("/Imports")]
    [HttpGet("/Imports/Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var batches = await imports.GetBatchesAsync(cancellationToken);
        return View(batches);
    }

    [HttpPost("/Imports/Upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "Выберите CSV-файл.");
        }
        else if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(file), "Нужен файл с расширением .csv.");
        }

        if (!ModelState.IsValid)
        {
            var batches = await imports.GetBatchesAsync(cancellationToken);
            return View("Index", batches);
        }

        await using var stream = file!.OpenReadStream();
        var result = await imports.ImportAsync(stream, file.FileName, cancellationToken);
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    [HttpGet("/Imports/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var batch = await imports.GetBatchAsync(id, cancellationToken);
        if (batch is null)
        {
            return NotFound();
        }

        var report = await reports.GetReportAsync(id, cancellationToken);
        ViewBag.Batch = batch;
        return View(report);
    }

    [HttpGet("/Imports/Records/{id:int}")]
    public async Task<IActionResult> Records(
        int id,
        QualityIssues? issue,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var batch = await imports.GetBatchAsync(id, cancellationToken);
        if (batch is null)
        {
            return NotFound();
        }

        var records = await reports.GetRecordsAsync(id, issue, page, pageSize: 50, cancellationToken);
        ViewBag.Batch = batch;
        ViewBag.Issue = issue;
        return View(records);
    }

    [HttpGet("/Imports/Duplicates/{id:int}")]
    public async Task<IActionResult> Duplicates(int id, CancellationToken cancellationToken)
    {
        var batch = await imports.GetBatchAsync(id, cancellationToken);
        if (batch is null)
        {
            return NotFound();
        }

        var groups = await deduplication.GetDuplicateGroupsAsync(id, cancellationToken);
        ViewBag.Batch = batch;
        return View(groups);
    }
}
