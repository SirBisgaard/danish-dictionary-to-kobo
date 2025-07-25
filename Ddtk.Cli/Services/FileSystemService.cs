using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ddtk.Cli.Helpers;
using Ddtk.Cli.Models;

namespace Ddtk.Cli.Services;

public class FileSystemService(AppSettings appSettings, LoggingService logger)
{
    private static readonly string[] Suffixes = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
    
    public Task<StreamWriter> GetJsonBackupStream()
    {
        var currentDir = AppContext.BaseDirectory;
        var path = Path.Combine(currentDir, appSettings.ExportJsonFileName);

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
        var fullPath = Path.Combine(currentDir, appSettings.ExportJsonFileName);
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
            
            File.AppendAllLines(testHtmlFile,words.Select(w => WordDefinitionHelper.ToKoboHtml(appSettings, w)), Encoding.UTF8);
            
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
        using var zipFs = new FileStream(appSettings.ExportKoboDictionaryFileName, FileMode.Create);
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

        logger.Log($" - Kobo dictionary ZIP file created: {appSettings.ExportKoboDictionaryFileName} ({ToReadableSize(zipFs.Length)})");
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