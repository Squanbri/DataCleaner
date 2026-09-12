using DataCleaner.Api.Services;

namespace DataCleaner.Tests.Deduplication;

// Generator lives with services; keep a small smoke test here or under its own folder.
public class SampleDataGeneratorTests
{
    [Fact]
    public void Generate_IncludesHeaderDirtyFormatsAndGarbage()
    {
        var generator = new SampleDataGenerator();
        var sample = generator.Generate(new SampleGenerationOptions
        {
            BasePeople = 50,
            DirtyCopyRate = 0.5,
            GarbageRate = 0.1,
            Seed = 42,
        });

        Assert.StartsWith("external_id;full_name;phone;email;birth_date;city", sample.CustomersCsv);
        Assert.True(sample.TotalRows > 50);
        Assert.Contains("8 (", sample.CustomersCsv);
        Assert.Contains("31.02.1990", sample.CustomersCsv);
        Assert.Contains("not-an-email", sample.CustomersCsv);
        Assert.StartsWith("truth_id;row_numbers", sample.TruthCsv);
        Assert.True(sample.DuplicateTruthGroups > 0);
    }
}
