using Ddtk.DataAccess.Models;
using Ddtk.Domain.Models;

namespace Ddtk.DataAccess.Interfaces;

public interface IFileSystemRepository
{
    Task<IList<string>> LoadSeedingWords();
    Task<DataFileInfo> GetSeedingWordsFileInfo();
    Task<IList<WordDefinition>> LoadWordDefinitions();
    Task<DataFileInfo> GetWordDefinitionsFileInfo();
    Task<DataFileInfo> GetKoboDictionaryFileInfo();
}