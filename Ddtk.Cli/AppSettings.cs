using System.Globalization;

namespace Ddtk.Cli;

public class AppSettings
{
    public CultureInfo Culture { get; set; } = new("da-DK");
    public string LogFileName { get; set; } = string.Empty;
    public string SeedingWordsFileName { get; set; } = string.Empty;
    public string ExportJsonFileName { get; set; } = string.Empty;
    public string ExportKoboDictionaryFileName { get; set; } = string.Empty;
    public string ExportKoboDictionaryTestHtmlFileName { get; set; } = string.Empty;
    public string DictionaryCopyRightText { get; set; } = string.Empty;
    public Uri WebScraperBaseAddress { get; set; } = new("https://localhost");
    public Uri WebScraperWordAddress { get; set; } = new("https://localhost");
    public Uri WebScraperStartUrl { get; set; } = new("https://localhost");

    public int WebScraperWorkerCount { get; set; }
}