using System.IO.Compression;
using System.Text.RegularExpressions;
using Ddtk.Business.Interfaces;
using Ddtk.Business.Models;
using Ddtk.DataAccess.Interfaces;
using Ddtk.Domain;
using HtmlAgilityPack;

namespace Ddtk.Business.Services;

public class SeedingWordService(IFileSystemRepository fileSystemRepository) : ISeedingWordService
{
    public async Task<string> GetSeedingWordsFileName()
    {
        var fileInfo = await fileSystemRepository.GetSeedingWordsFileInfo();
        return fileInfo.FileName;
    }

    public async Task<SeedingWordCollection> LoadSeedingWords()
    {
        var seedingWords = await fileSystemRepository.LoadSeedingWords();
        return new SeedingWordCollection(seedingWords);
    }

    public async Task SaveSeedingWords(SeedingWordCollection words)
    {
        await fileSystemRepository.SaveSeedingWords(words.Words);
    }

    public async Task<SeedingWordCollection> ExtractWordsFromFiles(string[] filePaths, IProgress<EpubExtractionProgress>? progress = null)
    {
        var seedingWordCollection = new SeedingWordCollection([]);

        var sortedPaths = filePaths.Where(p => p.EndsWith(".epub", StringComparison.OrdinalIgnoreCase)).ToArray();
        var filesProcessed = 0;

        foreach (var epubPath in sortedPaths)
        {
            progress?.Report(new EpubExtractionProgress
            {
                CurrentFile = Path.GetFileName(epubPath),
                FilesProcessed = filesProcessed,
                TotalFiles = filePaths.Length,
                TotalWords = seedingWordCollection.Count
            });

            try
            {
                var foundWords = await ExtractWordsFromSingleEpub(epubPath);
                seedingWordCollection.AddWords(foundWords);
            }
            catch (Exception)
            {
                // Continue processing other files even if one fails
                // Error will be logged/reported via progress
            }

            filesProcessed++;
        }

        progress?.Report(new EpubExtractionProgress
        {
            CurrentFile = sortedPaths.Last(),
            FilesProcessed = filesProcessed,
            TotalFiles = filePaths.Length,
            TotalWords = seedingWordCollection.Count
        });

        return seedingWordCollection;
    }

    /// <summary>
    /// Extracts words from a single EPUB file.
    /// </summary>
    private async Task<IEnumerable<string>> ExtractWordsFromSingleEpub(string epubPath)
    {
        var words = new List<string>();
        var tempFolder = Path.Combine(Path.GetTempPath(), $"epub_extract_{Guid.NewGuid():N}");

        try
        {
            // Extract EPUB (which is just a ZIP file)
            await Task.Run(() => ZipFile.ExtractToDirectory(epubPath, tempFolder));

            // Find all HTML/XHTML files
            var htmlFiles = Directory
                .EnumerateFiles(tempFolder, "*.xhtml", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(tempFolder, "*.html", SearchOption.AllDirectories));

            foreach (var htmlFile in htmlFiles)
            {
                await foreach (var word in ExtractWordsFromHtmlFile(htmlFile))
                {
                    words.Add(word);
                }
            }
        }
        finally
        {
            // Clean up temp folder
            try
            {
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        
        return words;
    }

    private readonly Regex wordRegex = new(@"\p{L}+", RegexOptions.Compiled);

    /// <summary>
    /// Extracts words from a single HTML file.
    /// </summary>
    private async IAsyncEnumerable<string> ExtractWordsFromHtmlFile(string htmlFile)
    {
        var document = new HtmlDocument();
        await Task.Run(() => document.Load(htmlFile));

        var textNodes = document.DocumentNode
            .Descendants()
            .Where(n => n.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(n.InnerText))
            .Select(n => n.InnerText);

        foreach (var textNode in textNodes)
        {
            var matches = wordRegex.Matches(textNode);
            foreach (Match match in matches)
            {
                yield return match.Value;
            }
        }
    }
}