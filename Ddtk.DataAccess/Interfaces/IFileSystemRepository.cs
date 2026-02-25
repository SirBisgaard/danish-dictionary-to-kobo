using Ddtk.DataAccess.Models;
using Ddtk.Domain.Models;

namespace Ddtk.DataAccess.Interfaces;

public interface IFileSystemRepository
{
    Task<IList<string>> LoadSeedingWords();
    Task<DataFileInfo> GetSeedingWordsFileInfo();
    Task SaveSeedingWords(IEnumerable<string> words);
    Task<IList<WordDefinition>> LoadWordDefinitions();
    Task<DataFileInfo> GetWordDefinitionsFileInfo();
    Task<DataFileInfo> GetKoboDictionaryFileInfo();
}