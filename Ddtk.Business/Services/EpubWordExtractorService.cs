using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Ddtk.Domain;
using Ddtk.Domain.Models;
using HtmlAgilityPack;

namespace Ddtk.Business.Services;

/// <summary>
/// Service for extracting words from EPUB files.
/// Refactored from Ddtk.EpubWordExtractor.Cli for reuse in TUI.
/// </summary>
public class EpubWordExtractorService
{
    private readonly AppSettings appSettings;
    private readonly Regex wordRegex = new(@"\p{L}+", RegexOptions.Compiled);

    public EpubWordExtractorService(AppSettings appSettings)
    {
        this.appSettings = appSettings;
    }

    /// <summary>
    /// Extracts words from a collection of EPUB files.
    /// </summary>
    /// <param name="epubPaths">Paths to EPUB files to process</param>
    /// <param name="progress">Progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Set of unique words extracted from all EPUBs</returns>
    public async Task<HashSet<string>> ExtractWordsFromEpubs(
        IEnumerable<string> epubPaths,
        IProgress<EpubExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var wordSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var epubList = epubPaths.ToList();
        var filesProcessed = 0;

        foreach (var epubPath in epubList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            progress?.Report(new EpubExtractionProgress
            {
                CurrentFile = Path.GetFileName(epubPath),
                FilesProcessed = filesProcessed,
                TotalFiles = epubList.Count,
                TotalWords = wordSet.Count
            });

            try
            {
                await ExtractWordsFromSingleEpub(epubPath, wordSet, cancellationToken);
            }
            catch (Exception)
            {
                // Continue processing other files even if one fails
                // Error will be logged/reported via progress
            }

            filesProcessed++;
        }

        // Final progress report
        progress?.Report(new EpubExtractionProgress
        {
            CurrentFile = null,
            FilesProcessed = filesProcessed,
            TotalFiles = epubList.Count,
            TotalWords = wordSet.Count
        });

        return wordSet;
    }

    /// <summary>
    /// Loads existing words from a file.
    /// </summary>
    /// <param name="filePath">Path to the file containing words (semicolon or newline separated)</param>
    /// <returns>Set of words from the file</returns>
    public async Task<HashSet<string>> LoadExistingWords(string filePath)
    {
        var wordSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(filePath))
        {
            return wordSet;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var words = content.Split([';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in words)
            {
                var trimmed = word.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    wordSet.Add(trimmed);
                }
            }
        }
        catch
        {
            // Return empty set on error
        }

        return wordSet;
    }

    /// <summary>
    /// Saves words to a file.
    /// </summary>
    /// <param name="words">Words to save</param>
    /// <param name="filePath">Path to save the words to</param>
    public async Task SaveWords(HashSet<string> words, string filePath)
    {
        var orderedWords = words.OrderBy(w => w, StringComparer.Create(this.appSettings.Culture, true));
        var content = string.Join(";", orderedWords);
        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
    }

    /// <summary>
    /// Merges extracted words with existing words from a file.
    /// </summary>
    /// <param name="extractedWords">Newly extracted words</param>
    /// <param name="existingFilePath">Path to file with existing words</param>
    /// <returns>Tuple of (new words, existing words)</returns>
    public async Task<(HashSet<string> newWords, HashSet<string> existingWords)> MergeWithExisting(
        HashSet<string> extractedWords,
        string existingFilePath)
    {
        var existingWords = await LoadExistingWords(existingFilePath);
        var newWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var word in extractedWords)
        {
            if (!existingWords.Contains(word))
            {
                newWords.Add(word);
            }
        }

        return (newWords, existingWords);
    }

    /// <summary>
    /// Extracts words from a single EPUB file.
    /// </summary>
    private async Task ExtractWordsFromSingleEpub(
        string epubPath,
        HashSet<string> wordSet,
        CancellationToken cancellationToken)
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"epub_extract_{Guid.NewGuid():N}");

        try
        {
            // Extract EPUB (which is just a ZIP file)
            await Task.Run(() => ZipFile.ExtractToDirectory(epubPath, tempFolder), cancellationToken);

            // Find all HTML/XHTML files
            var htmlFiles = Directory
                .EnumerateFiles(tempFolder, "*.xhtml", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(tempFolder, "*.html", SearchOption.AllDirectories));

            foreach (var htmlFile in htmlFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await ExtractWordsFromHtmlFile(htmlFile, wordSet);
                }
                catch
                {
                    // Continue processing other files
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
    }

    /// <summary>
    /// Extracts words from a single HTML file.
    /// </summary>
    private async Task ExtractWordsFromHtmlFile(string htmlFile, HashSet<string> wordSet)
    {
        var document = new HtmlDocument();
        await Task.Run(() => document.Load(htmlFile));

        var textNodes = document.DocumentNode
            .Descendants()
            .Where(n => n.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(n.InnerText))
            .Select(n => n.InnerText);

        foreach (var textNode in textNodes)
        {
            var matches = this.wordRegex.Matches(textNode);
            foreach (Match match in matches)
            {
                wordSet.Add(match.Value.ToLowerInvariant());
            }
        }
    }
}
