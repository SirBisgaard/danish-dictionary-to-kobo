namespace Ddtk.Domain.Models;

public class ScrapingProgress
{
    /// <summary>
    /// Number of words successfully scraped so far.
    /// </summary>
    public int WordsScraped { get; set; }
    
    /// <summary>
    /// Total number of words to scrape.
    /// </summary>
    public int TotalWords { get; set; }
    
    /// <summary>
    /// Current size of the work queue.
    /// </summary>
    public int QueueSize { get; set; }
    
    /// <summary>
    /// Time elapsed since scraping started.
    /// </summary>
    public TimeSpan Elapsed { get; set; }
    
    /// <summary>
    /// The word currently being processed (if available).
    /// </summary>
    public string? CurrentWord { get; set; }
    
    /// <summary>
    /// Log message for activity updates.
    /// </summary>
    public string? LogMessage { get; set; }
    
    /// <summary>
    /// Percentage of completion (0-100).
    /// </summary>
    public double PercentComplete => TotalWords > 0 ? (double)WordsScraped / TotalWords * 100 : 0;
}
