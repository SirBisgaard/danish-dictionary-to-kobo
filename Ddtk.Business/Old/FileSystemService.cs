using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ddtk.Business.Helpers;
using Ddtk.DataAccess;
using Ddtk.Domain;
using Ddtk.Domain.Models;

namespace Ddtk.Business.Services;

public class FileSystemService(AppSettings appSettings, LoggingService logger)
{
    private static readonly string[] Suffixes = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];

    public Task<StreamWriter> GetJsonBackupStream()
    {
        var currentDir = AppContext.BaseDirectory;
        var path = Path.Combine(currentDir, appSettings.WordDefinitionFileName);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        var streamWriter = new StreamWriter(fileStream, Encoding.UTF8);

        return Task.FromResult(streamWriter);
    }

    public async Task<List<WordDefinition>> LoadWordDefinitionsJson()
    {
        var currentDir = AppContext.BaseDirectory;
        var fullPath = Path.Combine(currentDir, appSettings.WordDefinitionFileName);
        if (!File.Exists(fullPath))
        {
            logger.Log($" - File not found: {fullPath}");
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(fullPath, Encoding.UTF8);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var definitions = JsonSerializer.Deserialize<List<WordDefinition>>(json, options);
            logger.Log($" - Loaded file from: {fullPath}");
            logger.Log($" - Found {definitions?.Count ?? 0} definitions in file.");

            return definitions ?? [];
        }
        catch (Exception ex)
        {
            logger.Log($" - Error reading file: {ex.Message}");
            return [];
        }
    }

    public async Task<string[]> LoadSeedingWords()
    {
        var currentDir = AppContext.BaseDirectory;
        var path = Path.Combine(currentDir, appSettings.SeedingWordsFileName);
        if (File.Exists(path))
        {
            var fileContent = await File.ReadAllTextAsync(path, Encoding.UTF8);
            var words = fileContent.Split(";");
            logger.Log($" - Seeding with {words.Length} words.");

            return words;
        }

        logger.Log($" - File not found: {path}");
        return [];
    }

    public async Task SaveHtmlDocument(string name, string document)
    {
        var currentDir = AppContext.BaseDirectory;
        var directory = Path.Combine(currentDir, "html-files");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        name = ToSafeFileName(name);

        var directoryPath = Path.Combine(currentDir, "html-files");
        var path = Path.Combine(directoryPath, name + ".html");
        if (File.Exists(path))
        {
            var index = 1;
            string candidate;
            do
            {
                candidate = Path.Combine(directoryPath, $"{name}-{index}.html");
                index++;
            } while (File.Exists(candidate));

            path = candidate;
        }

        await File.WriteAllTextAsync(path, document, Encoding.UTF8);
    }

    public void SaveKoboDictionary(List<WordDefinition> wordDefinitions)
    {
        var wordList = wordDefinitions.Select(w => w.Word).ToList();
        var prefixes = wordDefinitions.Select(w => w.WordPrefix).Distinct().ToList();

        // 1) Prepare a clean temp directory
        var tempDirectory = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "kobo_tmp"));
        if (tempDirectory.Exists)
        {
            logger.Log($" - Deleting existing temporary directory: {tempDirectory}");
            tempDirectory.Delete(true);
        }

        tempDirectory.Create();
        var testHtmlFile = Path.Combine(AppContext.BaseDirectory, appSettings.ExportKoboDictionaryTestHtmlFileName);
        if (File.Exists(testHtmlFile))
        {
            File.Delete(testHtmlFile);
        }

        // 2) For each prefix: write .raw.html then gzip→ .html
        logger.Log(" - Writing Kobo dictionary files.");
        foreach (var prefix in prefixes)
        {
            logger.LogOverwrite($" - Writing: {prefix}");
            var raw = Path.Combine(tempDirectory.FullName, $"{prefix}.raw.html");
            var words = wordDefinitions.Where(w => w.WordPrefix.Equals(prefix)).ToArray();
            var streamWriter = new StreamWriter(raw, true, Encoding.UTF8);
            foreach (var word in words)
            {
                streamWriter.WriteLine(WordDefinitionHelper.ToKoboHtml(appSettings, word));
            }

            streamWriter.Flush();
            streamWriter.Dispose();

            File.AppendAllLines(testHtmlFile, words.Select(w => WordDefinitionHelper.ToKoboHtml(appSettings, w)),
                Encoding.UTF8);

            var gz = Path.Combine(tempDirectory.FullName, $"{prefix}.html");
            using var fin = File.OpenRead(raw);
            using var fout = File.Create(gz);
            using var gzip = new GZipStream(fout, CompressionLevel.Optimal);
            fin.CopyTo(gzip);
        }

        // 3) Write the "words" index (plain newline‑separated)
        BuildKoboMarisaTrieWordLookupFile(wordList, Path.Combine(tempDirectory.FullName, "words"));


        // 4) Build the ZIP
        logger.Log(" - Creating Kobo dictionary ZIP file.");
        using var zipFs = new FileStream(appSettings.KoboDictionaryFileName, FileMode.Create);
        using var zip = new ZipArchive(zipFs, ZipArchiveMode.Create);

        var countFile = zip.CreateEntry("words.count");
        using (var w = new StreamWriter(countFile.Open(), Encoding.UTF8))
        {
            w.Write(wordList.Count.ToString());
        }

        var snapshotFile = zip.CreateEntry("words.snapshot");
        using (var w = new StreamWriter(snapshotFile.Open(), Encoding.UTF8))
        {
            w.Write(DateTime.UtcNow.ToString("yyyy-MM-dd"));
        }

        // 4b) Add all temp files at top‑level
        foreach (var file in tempDirectory.GetFiles())
        {
            zip.CreateEntryFromFile(file.FullName, file.Name);
        }

        tempDirectory.Delete(true);

        logger.Log(
            $" - Kobo dictionary ZIP file created: {appSettings.KoboDictionaryFileName} ({ToReadableSize(zipFs.Length)})");
        logger.Log($" - With {wordList.Count} words and {prefixes.Count} prefixes.");
    }

    private static void BuildKoboMarisaTrieWordLookupFile(IEnumerable<string> wordList, string outputPath)
    {
        var keys = wordList
            .Distinct()
            .OrderBy(w => w, StringComparer.Ordinal)
            .Select(w => Encoding.UTF8.GetBytes(w))
            .ToList();

        MarisaNative.BindMarisa();
        var builder = MarisaNative.CreateBuilder();
        foreach (var key in keys)
        {
            MarisaNative.PushIntoBuilder(builder, key, key.Length);
        }

        var trie = MarisaNative.BuildBuilder(builder);

        MarisaNative.DestroyBuilder(builder);
        MarisaNative.SaveTrie(trie, outputPath);
        MarisaNative.DestroyTrie(trie);
    }
    
    private static string ToSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "_";

        // Custom replacements for common non-ASCII letters to preserve readability
        var sb = new StringBuilder(name.Trim());

        // Normalize quotes and dashes to ASCII equivalents
        sb.Replace('“', '"').Replace('”', '"').Replace('„', '"').Replace('‟', '"');
        sb.Replace('–', '-').Replace('—', '-').Replace('−', '-');

        // Language specific
        sb.Replace("æ", "ae").Replace("Æ", "Ae");
        sb.Replace("ø", "oe").Replace("Ø", "Oe");
        sb.Replace("å", "aa").Replace("Å", "Aa");
        sb.Replace("ß", "ss");
        sb.Replace("œ", "oe").Replace("Œ", "Oe");
        sb.Replace("ð", "d").Replace("Ð", "D");
        sb.Replace("þ", "th").Replace("Þ", "Th");
        sb.Replace("ł", "l").Replace("Ł", "L");

        // Decompose and remove diacritics (convert, not delete letters)
        var normalized = sb.ToString().Normalize(NormalizationForm.FormD);
        var outSb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark && uc != UnicodeCategory.SpacingCombiningMark && uc != UnicodeCategory.EnclosingMark)
            {
                outSb.Append(ch);
            }
        }
        var asciiLike = outSb.ToString().Normalize(NormalizationForm.FormC);

        // Windows invalid chars
        var invalids = new HashSet<char>(Path.GetInvalidFileNameChars());

        var result = new StringBuilder();
        foreach (var ch in asciiLike)
        {
            // Allow letters, digits, and a small set of safe punctuation
            if (char.IsLetterOrDigit(ch))
            {
                result.Append(ch);
                continue;
            }

            switch (ch)
            {
                case ' ': result.Append('_'); break;
                case '-':
                case '_':
                case '.':
                case ',':
                case '+':
                case '=':
                case '!':
                case '@':
                case '#':
                case '$':
                case '%':
                case '&':
                case '(': 
                case ')': 
                    // Keep some benign punctuation
                    result.Append(ch); 
                    break;
                default:
                    if (invalids.Contains(ch) || ch < ' ')
                    {
                        result.Append('_');
                    }
                    else
                    {
                        // For any other symbol, map to underscore to avoid surprises
                        result.Append('_');
                    }
                    break;
            }
        }

        var safe = result.ToString();

        // Collapse multiple underscores
        while (safe.Contains("__")) 
            safe = safe.Replace("__", "_");

        // Trim dots and spaces from end (Windows restriction)
        safe = safe.Trim('.').Trim(' ');

        if (safe.Length == 0) 
            safe = "_";

        return safe;
    }

    private static string ToReadableSize(long byteCount)
    {
        if (byteCount < 0) return $"-{ToReadableSize(-byteCount)}";
        if (byteCount == 0) return "0 B";

        var suffixIndex = 0;
        double size = byteCount;

        while (size >= 1024 && suffixIndex < Suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:0.##} {Suffixes[suffixIndex]}";
    }
}