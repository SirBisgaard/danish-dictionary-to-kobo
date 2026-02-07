namespace Ddtk.Domain.Models;

public record DataStatus
{
    public int SeedingWordsCount { get; set; }
    public long SeedingWordsFileSize { get; set; }
    public DateTime? SeedingWordsFileChangeDate { get; set; }
    public required string SeedingFileName { get; set; } 
    
    public int WordDefinitionCount { get; set; }
    public long WordDefinitionFileSize { get; set; }
    public DateTime? WordDefinitionFileChangeDate { get; set; }
    public required string WordDefinitionFileName { get; set; }

    
    public long KoboDictionaryFileSize { get; set; }
    public DateTime? KoboDictionaryFileChangeDate { get; set; }
    public required string KoboDictionaryFileName { get; set; }
}