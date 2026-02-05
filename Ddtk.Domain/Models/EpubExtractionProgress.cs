namespace Ddtk.Domain.Models;

public class EpubExtractionProgress
{
    /// <summary>
    /// Path to the EPUB file currently being processed.
    /// </summary>
    public string? CurrentFile { get; set; }
    
    /// <summary>
    /// Number of EPUB files processed so far.
    /// </summary>
    public int FilesProcessed { get; set; }
    
    /// <summary>
    /// Total number of EPUB files to process.
    /// </summary>
    public int TotalFiles { get; set; }
    
    /// <summary>
    /// Total unique words extracted across all files.
    /// </summary>
    public int TotalWords { get; set; }
    
    /// <summary>
    /// Number of new words (not in existing seeding words).
    /// </summary>
    public int NewWords { get; set; }
    
    /// <summary>
    /// Number of words already known (in existing seeding words).
    /// </summary>
    public int ExistingWords { get; set; }
    
    /// <summary>
    /// Percentage of completion (0-100).
    /// </summary>
    public double PercentComplete => TotalFiles > 0 ? (double)FilesProcessed / TotalFiles * 100 : 0;
}
