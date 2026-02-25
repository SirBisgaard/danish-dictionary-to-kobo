using Ddtk.Business.Interfaces;
using Ddtk.DataAccess.Interfaces;
using Ddtk.Domain.Models;

namespace Ddtk.Business.Services;

public class DataStatusService(IFileSystemRepository fileSystemRepository, ISeedingWordService seedingWordService)
{
    public async Task<DataStatus> GetCurrentStatus()
    {
        var seedingWords = await seedingWordService.LoadSeedingWords();
        var seedingWordsFileInfo = await fileSystemRepository.GetSeedingWordsFileInfo();
        
        var wordDefinitions = await fileSystemRepository.LoadWordDefinitions();
        var wordDefinitionsFileInfo = await fileSystemRepository.GetWordDefinitionsFileInfo();
        var koboDictionaryFileInfo = await fileSystemRepository.GetKoboDictionaryFileInfo();

        return new DataStatus
        {
            SeedingWordsCount = seedingWords.Count,
            SeedingWordsFileSize = seedingWordsFileInfo.FileSize,
            SeedingWordsFileChangeDate = seedingWordsFileInfo.FileChangeDate,
            SeedingFileName = seedingWordsFileInfo.FileName,
            
            WordDefinitionCount = wordDefinitions.Count,
            WordDefinitionFileSize = wordDefinitionsFileInfo.FileSize,
            WordDefinitionFileChangeDate = wordDefinitionsFileInfo.FileChangeDate,
            WordDefinitionFileName = wordDefinitionsFileInfo.FileName,
            
            KoboDictionaryFileSize = koboDictionaryFileInfo.FileSize,
            KoboDictionaryFileChangeDate = koboDictionaryFileInfo.FileChangeDate,
            KoboDictionaryFileName = koboDictionaryFileInfo.FileName
        };
    }
}