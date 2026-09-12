using System.Text;
using DataCleaner.Api.Services;
using DataCleaner.Api.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DataCleaner.Api.Controllers;

public class SamplesController(SampleDataGenerator generator) : Controller
{
    [HttpGet("/Samples")]
    [HttpGet("/Samples/Generate")]
    public IActionResult Generate()
    {
        return View(new GenerateSampleViewModel());
    }

    [HttpPost("/Samples/Generate")]
    [ValidateAntiForgeryToken]
    public IActionResult Generate(GenerateSampleViewModel model, string download)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var options = new SampleGenerationOptions
            {
                BasePeople = model.BasePeople,
                DirtyCopyRate = model.DirtyCopyPercent / 100.0,
                GarbageRate = model.GarbagePercent / 100.0,
                Seed = model.Seed,
            };

            var sample = generator.Generate(options);

            if (string.Equals(download, "truth", StringComparison.OrdinalIgnoreCase))
            {
                return File(
                    Encoding.UTF8.GetBytes(sample.TruthCsv),
                    "text/csv; charset=utf-8",
                    "customers.truth.csv");
            }

            return File(
                Encoding.UTF8.GetBytes(sample.CustomersCsv),
                "text/csv; charset=utf-8",
                "customers.csv");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
