namespace Ddtk.Domain.Models;

public class ProcessingProgress
{
    /// <summary>
    /// Number of word definitions processed so far.
    /// </summary>
    public int ProcessedCount { get; set; }
    
    /// <summary>
    /// Total number of word definitions to process.
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Current status message.
    /// </summary>
    public string? Status { get; set; }
    
    /// <summary>
    /// Percentage of completion (0-100).
    /// </summary>
    public double PercentComplete => TotalCount > 0 ? (double)ProcessedCount / TotalCount * 100 : 0;
}
