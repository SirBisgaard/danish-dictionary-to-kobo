using Ddtk.Business.Models;
using Ddtk.Domain;

namespace Ddtk.Business.Interfaces;

public interface ISeedingWordService
{
    public Task<string> GetSeedingWordsFileName();
    public Task<SeedingWordCollection> LoadSeedingWords();
    public Task SaveSeedingWords(SeedingWordCollection words);

    /// <summary>
    /// Can only handle EPUB files.
    /// </summary>
    /// <param name="filePaths"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public Task<SeedingWordCollection> ExtractWordsFromFiles(string[] filePaths, IProgress<EpubExtractionProgress>? progress = null);
}