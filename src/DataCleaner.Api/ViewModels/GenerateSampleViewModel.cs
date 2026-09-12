using System.ComponentModel.DataAnnotations;

namespace DataCleaner.Api.ViewModels;

public sealed class GenerateSampleViewModel
{
    [Display(Name = "Эталонных людей")]
    [Range(10, 5000)]
    public int BasePeople { get; set; } = 500;

    [Display(Name = "Доля с грязными копиями, %")]
    [Range(0, 100)]
    public int DirtyCopyPercent { get; set; } = 20;

    [Display(Name = "Доля мусорных строк, %")]
    [Range(0, 20)]
    public int GarbagePercent { get; set; } = 3;

    [Display(Name = "Seed (для повторяемости)")]
    public int Seed { get; set; } = 20260912;
}
