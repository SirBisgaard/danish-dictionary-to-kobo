namespace Ddtk.Business.Models;

public record EpubExtractionProgress
{
    /// <summary>
    /// Path to the EPUB file currently being processed.
    /// </summary>
    public required string CurrentFile { get; set; }
    public int FilesProcessed { get; set; }
    public int TotalFiles { get; set; }
    public int TotalWords { get; set; }
}
