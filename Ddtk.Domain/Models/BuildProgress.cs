namespace Ddtk.Domain.Models;

public class BuildProgress
{
    /// <summary>
    /// Number of prefixes processed so far.
    /// </summary>
    public int CurrentPrefix { get; set; }
    
    /// <summary>
    /// Total number of prefixes to process.
    /// </summary>
    public int TotalPrefixes { get; set; }
    
    /// <summary>
    /// Current status message.
    /// </summary>
    public string? Status { get; set; }
    
    /// <summary>
    /// Name of the prefix currently being processed.
    /// </summary>
    public string? CurrentPrefixName { get; set; }
    
    /// <summary>
    /// Percentage of completion (0-100).
    /// </summary>
    public double PercentComplete => TotalPrefixes > 0 ? (double)CurrentPrefix / TotalPrefixes * 100 : 0;
}
