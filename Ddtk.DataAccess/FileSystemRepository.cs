using System.Text.Encodings.Web;
using System.Text.Json;
using Ddtk.DataAccess.Interfaces;
using Ddtk.DataAccess.Models;
using Ddtk.Domain;
using Ddtk.Domain.Models;

namespace Ddtk.DataAccess;

public class FileSystemRepository(AppSettings appSettings) : IFileSystemRepository
{
    private readonly FileInfo seedingWordsFileInfo = new(Path.Combine(AppContext.BaseDirectory, appSettings.SeedingWordsFileName));
    private readonly FileInfo wordDefinitionsFileInfo = new(Path.Combine(AppContext.BaseDirectory, appSettings.WordDefinitionFileName));
    private readonly FileInfo koboDictionaryFileInfo = new(Path.Combine(AppContext.BaseDirectory, appSettings.KoboDictionaryFileName));

    public async Task<IList<string>> LoadSeedingWords()
    {
        seedingWordsFileInfo.Refresh();
        if (seedingWordsFileInfo.Exists is false)
        {
            return new List<string>(0);
        }

        return await LoadFile<IList<string>>(seedingWordsFileInfo);
    }

    public Task<DataFileInfo> GetSeedingWordsFileInfo()
    {
        seedingWordsFileInfo.Refresh();

        return Task.FromResult(new DataFileInfo
        {
            FileName = seedingWordsFileInfo.Name,
            FileSize = seedingWordsFileInfo.Exists ? seedingWordsFileInfo.Length : 0,
            FileChangeDate = seedingWordsFileInfo.Exists ? seedingWordsFileInfo.LastWriteTimeUtc : null,
        });
    }

    public Task SaveSeedingWords(IEnumerable<string> words)
    {
        return SaveFile(seedingWordsFileInfo, words.ToList());
    }

    public async Task<IList<WordDefinition>> LoadWordDefinitions()
    {
        wordDefinitionsFileInfo.Refresh();
        if (wordDefinitionsFileInfo.Exists is false)
        {
            return new List<WordDefinition>(0);
        }

        return await LoadFile<IList<WordDefinition>>(wordDefinitionsFileInfo);
    }

    public Task<DataFileInfo> GetWordDefinitionsFileInfo()
    {
        wordDefinitionsFileInfo.Refresh();

        return Task.FromResult(new DataFileInfo
        {
            FileName = wordDefinitionsFileInfo.Name,
            FileSize = wordDefinitionsFileInfo.Exists ? wordDefinitionsFileInfo.Length : 0,
            FileChangeDate = wordDefinitionsFileInfo.Exists ? wordDefinitionsFileInfo.LastWriteTimeUtc : null,
        });
    }

    public Task<DataFileInfo> GetKoboDictionaryFileInfo()
    {
        wordDefinitionsFileInfo.Refresh();

        return Task.FromResult(new DataFileInfo
        {
            FileName = koboDictionaryFileInfo.Name,
            FileSize = koboDictionaryFileInfo.Exists ? koboDictionaryFileInfo.Length : 0,
            FileChangeDate = koboDictionaryFileInfo.Exists ? koboDictionaryFileInfo.LastWriteTimeUtc : null,
        });
    }

    private async Task<T> LoadFile<T>(FileInfo fileInfo)
    {
        await using var fileStream = File.OpenRead(fileInfo.FullName);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var deserializedObject = await JsonSerializer.DeserializeAsync<T>(fileStream, options);
        
        return deserializedObject ?? throw new ArgumentNullException(nameof(deserializedObject), "Deserialized object is null.");
    }

    private async Task SaveFile<T>(FileInfo fileInfo, T data)
    {
        if (fileInfo.Exists)
        {
            File.Delete(fileInfo.FullName);
        }
        
        await using var fileStream = File.OpenWrite(fileInfo.FullName);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        await JsonSerializer.SerializeAsync(fileStream, data, options);
    }
}