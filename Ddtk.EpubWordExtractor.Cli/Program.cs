using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

Console.WriteLine("Initializing Process");
Console.WriteLine();

long oldWords = 0;

var outputFile = Path.Combine(AppContext.BaseDirectory, "extracted_words.txt");
var tempFolder = Path.Combine(AppContext.BaseDirectory, "_temp");
var wordSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var wordRegex = new Regex(@"\p{L}+", RegexOptions.Compiled);

// Seed the word set with previously known words
if (File.Exists(outputFile))
{
    var words = File.ReadAllText(outputFile).Split(";");
    oldWords = words.Length;
    foreach (var word in words)
    {
        wordSet.Add(word);
    }
}

Console.WriteLine("Extracting words from epub files in current directory.");
foreach (var epubPath in Directory.GetFiles(AppContext.BaseDirectory).Where(f => f.EndsWith(".epub")))
{
    Console.WriteLine($" - Extracting: {epubPath}");

    try
    {
        ZipFile.ExtractToDirectory(epubPath, tempFolder);

        // Find all .xhtml/.html files
        var htmlFiles = Directory
            .EnumerateFiles(tempFolder, "*.xhtml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(tempFolder, "*.html", SearchOption.AllDirectories));

        foreach (var file in htmlFiles)
        {
            try
            {
                var document = new HtmlDocument();
                document.Load(file);
                var text = document.DocumentNode
                    .Descendants()
                    .Where(n => n.NodeType == HtmlNodeType.Text &&
                                !string.IsNullOrWhiteSpace(n.InnerText))
                    .Select(n => n.InnerText)
                    .Aggregate((a, b) => $"{a} {b}");

                foreach (Match wordMatch in wordRegex.Matches(text))
                {
                    wordSet.Add(wordMatch.Value.ToLowerInvariant());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($" - Error: {e.Message}");
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($" - Error: {e.Message}");
    }
    finally
    {
        // Clean up
        Directory.Delete(tempFolder, true);
    }
}

Console.WriteLine();

var orderedWords = wordSet.OrderBy(w => w, StringComparer.Create(new CultureInfo("da-DK"), true));
var dataToWrite = string.Join(";", orderedWords);

// Output distinct words
File.WriteAllText(outputFile, dataToWrite, Encoding.UTF8);
Console.WriteLine("Successfully extracted epub words.");
Console.WriteLine($" - File: {outputFile}");
Console.WriteLine($" - Words: {wordSet.Count}");
Console.WriteLine($" - New: {wordSet.Count - oldWords}");
Console.WriteLine($" - Old: {oldWords}");
Console.WriteLine();


Console.WriteLine("Completed Process. Have a nice day. ✅");