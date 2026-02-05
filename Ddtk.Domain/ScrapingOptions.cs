namespace Ddtk.Domain;

/// <summary>
/// Runtime configuration options for web scraping operations.
/// These settings are not persisted to AppSettings and apply only to the current session.
/// </summary>
public class ScrapingOptions
{
    /// <summary>
    /// Whether to use the seeded words list as the initial scraping queue.
    /// </summary>
    public bool UseSeededWords { get; set; } = true;
    
    /// <summary>
    /// Whether to save raw HTML files for each scraped word.
    /// Useful for debugging and offline processing.
    /// </summary>
    public bool SaveHtmlFiles { get; set; } = false;
    
    /// <summary>
    /// Whether to update existing HTML files on rerun.
    /// Only applicable if SaveHtmlFiles is true.
    /// </summary>
    public bool UpdateExistingHtmlFiles { get; set; } = false;
    
    /// <summary>
    /// Whether to update the seeded words file with newly discovered words during scraping.
    /// This improves subsequent runs by expanding the seed list.
    /// </summary>
    public bool UpdateSeededWords { get; set; } = false;
    
    /// <summary>
    /// Number of concurrent worker threads for web scraping.
    /// Defaults to the value from AppSettings.WebScraperWorkerCount.
    /// </summary>
    public int WorkerCount { get; set; } = 8;
}
