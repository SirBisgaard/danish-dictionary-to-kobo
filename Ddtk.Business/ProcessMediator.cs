using Ddtk.Business.Helpers;
using Ddtk.Business.Services;
using Ddtk.Domain;

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

    public async ValueTask DisposeAsync()
    {
        await logger.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}