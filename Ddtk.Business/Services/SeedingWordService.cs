using Ddtk.Business.Interfaces;
using Ddtk.Business.Models;
using Ddtk.Domain;

namespace Ddtk.Business.Services;

public class SeedingWordService : ISeedingWordService
{
    public string GetSeedingWordsFileName()
    {
        throw new NotImplementedException();
    }

    public Task<SeedingWordCollection> LoadSeedingWords()
    {
        throw new NotImplementedException();
    }

    public Task SaveSeedingWords(SeedingWordCollection words)
    {
        throw new NotImplementedException();
    }

    public Task<string[]> ExtractWordsFromFiles(string[] filePaths, IProgress<EpubExtractionProgress>? progress = null)
    {
        throw new NotImplementedException();
    }
}