using Ddtk.Business.Helpers;
using Ddtk.Business.Services;
using Ddtk.Domain;
using Ddtk.Domain.Models;

namespace Ddtk.Business;

public class ProcessMediator : IAsyncDisposable
{
    private readonly AppSettings appSettings;
    private readonly LoggingService logger;
    private readonly FileSystemService fileSystemService;

    public ProcessMediator(AppSettings appSettings)
    {
        this.appSettings = appSettings;
        logger = new LoggingService(appSettings);
        fileSystemService = new FileSystemService(appSettings, logger);
    }

    #region Step-by-Step Methods for TUI

    /// <summary>
    /// Loads seeding words from the configured file.
    /// </summary>
    public async Task<string[]> LoadSeedingWords()
    {
        logger.Log("Loading seeding words.");
        var seedingWords = await fileSystemService.LoadSeedingWords();
        logger.Log($" - Loaded {seedingWords.Length} seeding words.");
        return seedingWords;
    }

    /// <summary>
    /// Loads word definitions from the JSON file.
    /// </summary>
    public async Task<List<WordDefinition>> LoadWordDefinitionsJson()
    {
        logger.Log("Loading word definitions from JSON.");
        var wordDefinitions = await fileSystemService.LoadWordDefinitionsJson();
        logger.Log($" - Loaded {wordDefinitions.Count} word definitions.");
        return wordDefinitions;
    }

    /// <summary>
    /// Calculates the number of words remaining to scrape (seeding words minus already processed).
    /// </summary>
    public async Task<int> GetRemainingWordsToScrape()
    {
        var seedingWords = await fileSystemService.LoadSeedingWords();
        var wordDefinitions = await fileSystemService.LoadWordDefinitionsJson();
        
        var processedSet = new HashSet<string>(
            wordDefinitions.Select(wd => wd.Word.ToLowerInvariant()), 
            StringComparer.OrdinalIgnoreCase);
        
        var remaining = seedingWords.Where(sw => !processedSet.Contains(sw)).Count();
        
        return remaining;
    }

    /// <summary>
    /// Runs the web scraping process with progress reporting.
    /// </summary>
    public async Task<List<WordDefinition>> RunScraping(
        string[] seedingWords,
        ScrapingOptions options,
        IProgress<ScrapingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        logger.Log("Starting web scraping process.");
        logger.Log($" - Seeding with {seedingWords.Length} words.");
        logger.Log($" - Worker count: {options.WorkerCount}");
        logger.Log();

        // Filter out already processed words
        var wordDefinitions = await fileSystemService.LoadWordDefinitionsJson();
        var processedSet = new HashSet<string>(
            wordDefinitions.Select(wd => wd.Word.ToLowerInvariant()), 
            StringComparer.OrdinalIgnoreCase);
        
        var wordsToScrape = seedingWords.Where(sw => !processedSet.Contains(sw)).ToArray();
        logger.Log($" - Missing {wordsToScrape.Length} words from seeding.");
        logger.Log();

        try
        {
            await using var jsonBackupStream = await fileSystemService.GetJsonBackupStream();
            await using var backupService = new BackupService(jsonBackupStream, fileSystemService);
            
            // Create scraper with updated worker count from options
            var modifiedSettings = new AppSettings
            {
                Culture = appSettings.Culture,
                LogFileName = appSettings.LogFileName,
                SeedingWordsFileName = appSettings.SeedingWordsFileName,
                ExportJsonFileName = appSettings.ExportJsonFileName,
                ExportKoboDictionaryFileName = appSettings.ExportKoboDictionaryFileName,
                ExportKoboDictionaryTestHtmlFileName = appSettings.ExportKoboDictionaryTestHtmlFileName,
                DictionaryCopyRightText = appSettings.DictionaryCopyRightText,
                WebScraperBaseAddress = appSettings.WebScraperBaseAddress,
                WebScraperWordAddress = appSettings.WebScraperWordAddress,
                WebScraperStartUrl = appSettings.WebScraperStartUrl,
                WebScraperWorkerCount = options.WorkerCount
            };

            await using var scraper = new WordDefinitionWebScraperService(modifiedSettings, logger, backupService);

            await scraper.ScrapeWordDefinitions(wordsToScrape);
            wordDefinitions = scraper.WordDefinitions;

            logger.Log($" - Scraping completed with {wordDefinitions.Count} word definitions.");
        }
        catch (Exception e)
        {
            logger.Log($" - Error during web scraping: {e.Message}");
            throw;
        }

        logger.Log();
        return wordDefinitions;
    }

    /// <summary>
    /// Processes raw word definitions (merging, cleaning, sorting).
    /// </summary>
    public async Task<List<WordDefinition>> RunProcessing(
        List<WordDefinition> definitions,
        IProgress<ProcessingProgress>? progress = null)
    {
        logger.Log("Preparing word definitions for Kobo dictionary.");
        
        progress?.Report(new ProcessingProgress
        {
            ProcessedCount = 0,
            TotalCount = definitions.Count,
            Status = "Processing word definitions..."
        });

        var preparedDefinitions = WordDefinitionHelper.ToPreparedWordDefinitions(appSettings, logger, definitions);
        
        progress?.Report(new ProcessingProgress
        {
            ProcessedCount = preparedDefinitions.Count,
            TotalCount = preparedDefinitions.Count,
            Status = "Processing complete"
        });

        logger.Log();
        return await Task.FromResult(preparedDefinitions);
    }

    /// <summary>
    /// Builds the final Kobo dictionary ZIP file from word definitions.
    /// </summary>
    public async Task RunBuild(
        List<WordDefinition> definitions,
        IProgress<BuildProgress>? progress = null)
    {
        if (definitions.Count == 0)
        {
            logger.Log("No word definitions were found to create Kobo dictionary.");
            logger.Log();
            return;
        }

        logger.Log("Transforming the word definitions into Kobo dictionary.");
        
        progress?.Report(new BuildProgress
        {
            CurrentPrefix = 0,
            TotalPrefixes = definitions.Select(w => w.WordPrefix).Distinct().Count(),
            Status = "Building Kobo dictionary..."
        });

        fileSystemService.SaveKoboDictionary(definitions);
        
        progress?.Report(new BuildProgress
        {
            CurrentPrefix = definitions.Select(w => w.WordPrefix).Distinct().Count(),
            TotalPrefixes = definitions.Select(w => w.WordPrefix).Distinct().Count(),
            Status = "Build complete"
        });

        logger.Log();
    }

    #endregion

    #region Original Monolithic Method (for CLI compatibility)

    /// <summary>
    /// Original monolithic run method for CLI mode.
    /// Kept for backward compatibility.
    /// </summary>
    public async Task Run(bool skipWebScraping)
    {
        logger.Log("Initializing Process.");
        logger.Log();

        logger.Log("Checking if word definitions and seeding words exist.");
        var seedingWords = await fileSystemService.LoadSeedingWords();
        var wordDefinitions = await fileSystemService.LoadWordDefinitionsJson();

        var processedSet = new HashSet<string>(wordDefinitions.Select(wd => wd.Word.ToLowerInvariant()), StringComparer.OrdinalIgnoreCase);
        seedingWords = seedingWords.Where(sw => !processedSet.Contains(sw)).ToArray();
        logger.Log($" - Missing {seedingWords.Length} words from seeding.");
        logger.Log();

        if (!skipWebScraping)
        {
            logger.Log("Web Scrapper is starting.");
            try
            {
                await using var jsonBackupStream = await fileSystemService.GetJsonBackupStream();
                await using var backupService = new BackupService(jsonBackupStream, fileSystemService);
                await using var scraper = new WordDefinitionWebScraperService(appSettings, logger, backupService);

                await scraper.ScrapeWordDefinitions(seedingWords);
                wordDefinitions = scraper.WordDefinitions;
            }
            catch (Exception e)
            {
                logger.Log($" - Error during web scraping: {e.Message}");
            }
        }
        else
        {
            logger.Log("Skipping web scraping as per user request.");
        }
        logger.Log();

        logger.Log("Preparing word definitions for Kobo dictionary.");
        wordDefinitions = WordDefinitionHelper.ToPreparedWordDefinitions(appSettings, logger, wordDefinitions);
        logger.Log();

        if (wordDefinitions.Count != 0)
        {
            logger.Log("Transforming the word definitions into Kobo dictionary.");
            fileSystemService.SaveKoboDictionary(wordDefinitions);
            logger.Log();
        }
        else
        {
            logger.Log("No word definitions were found to create Kobo dictionary.");
            logger.Log();
        }

        logger.Log("Completed Process. Have a nice day. ✅");
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        await logger.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}
