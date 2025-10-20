using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading.Channels;
using System.Web;
using Ddtk.Cli.Helpers;
using Ddtk.Cli.Models;
using HtmlAgilityPack;
using Timer = System.Timers.Timer;

namespace Ddtk.Cli.Services;

public class WordDefinitionWebScraperService(AppSettings appSettings, LoggingService logger, BackupService backupService) : IAsyncDisposable
{
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();
    private readonly Timer timer = new();

    private readonly ConcurrentDictionary<string, WordDefinition> processedWords = new();
    private readonly ConcurrentDictionary<string, string> seenLinks = new();

    private long queueCounter;
    private readonly Channel<string> channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });
    private readonly Task[] workers = new Task[appSettings.WebScraperWorkerCount];

    public async Task ScrapeWordDefinitions(List<WordDefinition> knownWordDefinitions, string[] seedingWords)
    {
        logger.Log($" - Starting to scrape at: {appSettings.WebScraperBaseAddress}");
        await SeedChannel(seedingWords);

        for (var i = 0; i < workers.Length; i++)
        {
            workers[i] = Task.Run(ProcessUrlTask);
        }

        logger.Log($" - Started {appSettings.WebScraperWorkerCount} workers.");
        timer.Enabled = true;
        timer.Interval = 30_000;
        timer.Elapsed += (_, _) => logger.LogOverwrite($" - [W:{processedWords.Count}][Q:{channel.Reader.Count}][T:{stopwatch.Elapsed:hh\\:mm\\:ss}]");
        logger.LogOverwrite($" - [W:{processedWords.Count}][Q:{channel.Reader.Count}][T:{stopwatch.Elapsed:hh\\:mm\\:ss}]");

        await Task.WhenAll(workers);

        logger.Log($" - Scraped {processedWords.Count} word definitions!");
    }

    public List<WordDefinition> WordDefinitions => processedWords.Values.ToList();

    public async ValueTask DisposeAsync()
    {
        timer.Dispose();
        channel.Writer.Complete();
        
        await Task.WhenAll(workers.OfType<Task>());

        GC.SuppressFinalize(this);
    }

    private async Task ProcessUrlTask()
    {
        using var client = new HttpClient();
        client.BaseAddress = appSettings.WebScraperBaseAddress;
        var r = new Random();

        await foreach (var currentLink in channel.Reader.ReadAllAsync())
        {
            try
            {
                await Task.Delay(r.Next(800, 1500));

                var parsedWordAndLink = ParseLink(currentLink);
                if (parsedWordAndLink is not null && processedWords.ContainsKey(parsedWordAndLink.Value.word))
                {
                    continue;
                }

                var html = await client.GetStringAsync(currentLink);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var wordsAndLinks = ExtractLinks(htmlDocument);
                foreach (var wordAndLink in wordsAndLinks)
                {
                    if (processedWords.ContainsKey(wordAndLink.word))
                    {
                        continue;
                    }

                    if (seenLinks.TryAdd(wordAndLink.word, wordAndLink.link))
                    {
                        Interlocked.Increment(ref queueCounter);
                        await channel.Writer.WriteAsync(wordAndLink.link);
                    }
                }

                if (parsedWordAndLink is null)
                {
                    continue;
                }

                var wordDefinition = WordDefinitionHelper.FromHtml(htmlDocument);
                if (wordDefinition is not null && processedWords.TryAdd(wordDefinition.Word.ToLower(), wordDefinition))
                {
                    await backupService.AddToQueue(wordDefinition, html);

                    if (processedWords.Count % 1000 == 0)
                    {
                        logger.LogOverwrite($" - [W:{processedWords.Count}][Q:{channel.Reader.Count}][T:{stopwatch.Elapsed:hh\\:mm\\:ss}]");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException hre)
                {
                    if (hre.StatusCode is null)
                    {
                        logger.Log($" - Fatal Error: {ex.Message}");
                        return;
                    }
                    
                    if (hre.StatusCode != HttpStatusCode.NotFound)
                    {
                        logger.Log($" - Error: {currentLink} - {(int)hre.StatusCode!}:{hre.StatusCode}");
                    }

                    if (hre.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable)
                    { 
                        logger.Log($" - Requeue: {currentLink}");
                    }
                }
                else
                {
                    logger.Log($" - Unknown Error: {ex.Message}{Environment.NewLine}{ex}");
                }
            }
            finally
            {
                Interlocked.Decrement(ref queueCounter);
                if (queueCounter == 0)
                {
                    // only one thread ever sees zero
                    channel.Writer.Complete();
                }
            }
        }
    }

    private IEnumerable<(string link, string word)> ExtractLinks(HtmlDocument document)
    {
        var rawLinks = document.DocumentNode
            .SelectNodes("//a[@href]")?
            .Select(a => a.GetAttributeValue("href", "")) ?? [];

        var links = rawLinks
            .Where(l => l.StartsWith(appSettings.WebScraperWordAddress.ToString(), StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(ParseLink)
            .OfType<(string link, string word)>()
            .DistinctBy(l => l.word, StringComparer.OrdinalIgnoreCase);

        return links;
    }

    private (string link, string word)? ParseLink(string link)
    {
        try
        {
            var uri = new Uri(link);

            var queryString = HttpUtility.ParseQueryString(WebUtility.UrlDecode(HttpUtility.HtmlDecode(uri.Query)));

            var queryWord = queryString["query"]?.ToLower();
            var selectWord = queryString["aselect"]?.ToLower();
            if (queryWord is null)
            {
                return null;
            }

            if (selectWord is not null && !selectWord.Equals(queryWord))
            {
                queryWord = selectWord;
            }

            var parsedLink = $"{appSettings.WebScraperWordAddress}?query={WebUtility.UrlEncode(queryWord)}";

            if (string.IsNullOrWhiteSpace(parsedLink) || string.IsNullOrWhiteSpace(queryWord))
            {
                return null;
            }

            return (parsedLink, queryWord);
        }
        catch (Exception e)
        {
            logger.Log($" - Error: {e.Message} URL: {link}");
            return null;
        }
    }

    private async Task SeedChannel(string[] seedingWords)
    {
        Interlocked.Increment(ref queueCounter);
        await channel.Writer.WriteAsync(appSettings.WebScraperStartUrl.ToString());

        logger.Log($" - Seeded: {seedingWords.Length} words.");
        foreach (var seedingWord in seedingWords)
        {
            Interlocked.Increment(ref queueCounter);
            await channel.Writer.WriteAsync($"{appSettings.WebScraperWordAddress}?query={WebUtility.UrlEncode(seedingWord)}");
            seenLinks.TryAdd(seedingWord, seedingWord);
        }
    }
}