namespace Ddtk.Domain.Models;

public struct DataStatus
{
    public int SeedingWordsCount { get; set; }
    public long SeedingWordsFileSize { get; set; }
    public DateTime? SeedingWordsFileChangeDate { get; set; }
    public string SeedingFileName { get; set; } 
    
    public int WordDefinitionCount { get; set; }
    public long WordDefinitionFileSize { get; set; }
    public DateTime? WordDefinitionFileChangeDate { get; set; }
    public string WordDefinitionFileName { get; set; }

    
    public long KoboDictionaryFileSize { get; set; }
    public DateTime? KoboDictionaryFileChangeDate { get; set; }
    public string KoboDictionaryFileName { get; set; }
}