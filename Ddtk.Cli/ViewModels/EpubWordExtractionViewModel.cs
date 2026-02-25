using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using System.Text;
using Ddtk.Business.Interfaces;
using Ddtk.Business.Models;
using Ddtk.Domain;
using ReactiveUI;

namespace Ddtk.Cli.ViewModels;

public class EpubWordExtractionViewModel : ViewModelBase
{
    private readonly ISeedingWordService seedingWordService;

    public ObservableCollection<string> SelectedFiles
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    public string FilesFrameTitle
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Selected Files (0)";

    public float ProgressFraction
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = 0f;

    public string ProgressText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Ready";

    public string Statistics
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public bool CanExtract
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = false;

    public bool CanSave
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = false;

    public bool CanViewWords
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = false;

    public bool CanExport
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = false;

    private SeedingWordCollection extractedWords = new([]);
    private bool isExtracting;
    
    // Events for file dialogs (Views will handle these)
    public event EventHandler? SelectFilesRequested;
    public event EventHandler? SelectFolderRequested;
    public event EventHandler<ViewWordsEventArgs>? ViewWordsRequested;
    
    // Commands
    public ReactiveCommand<Unit, Unit> SelectFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearSelectionCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractWordsCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveToSeedingWordsCommand { get; }
    public ReactiveCommand<Unit, Unit> ViewWordsCommand { get; }
    
    public EpubWordExtractionViewModel(ISeedingWordService seedingWordService)
    {
        this.seedingWordService = seedingWordService;
        
        SelectFilesCommand = ReactiveCommand.Create(() => SelectFilesRequested?.Invoke(this, EventArgs.Empty));
        SelectFolderCommand = ReactiveCommand.Create(() => SelectFolderRequested?.Invoke(this, EventArgs.Empty));
        ClearSelectionCommand = ReactiveCommand.Create(ClearSelection);
        ExtractWordsCommand = ReactiveCommand.CreateFromTask(ExtractWordsAsync, 
            this.WhenAnyValue(vm => vm.CanExtract));
        SaveToSeedingWordsCommand = ReactiveCommand.CreateFromTask(SaveToSeedingWordsAsync,
            this.WhenAnyValue(vm => vm.CanSave));
        ViewWordsCommand = ReactiveCommand.Create(ViewWords,
            this.WhenAnyValue(vm => vm.CanViewWords));
    }
    
    public void AddFiles(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (filePath.EndsWith(".epub", StringComparison.OrdinalIgnoreCase) && 
                !SelectedFiles.Contains(filePath))
            {
                SelectedFiles.Add(filePath);
            }
        }
        
        UpdateFilesList();
    }
    
    public void AddFilesFromFolder(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            var epubFiles = Directory.GetFiles(folderPath, "*.epub", SearchOption.AllDirectories);

            foreach (var epubFile in epubFiles)
            {
                if (!SelectedFiles.Contains(epubFile))
                {
                    SelectedFiles.Add(epubFile);
                }
            }
            
            UpdateFilesList();
        }
        else
        {
            // Treat as single file selection
            if (!folderPath.EndsWith(".epub", StringComparison.OrdinalIgnoreCase) || SelectedFiles.Contains(folderPath))
            {
                return;
            }
            
            SelectedFiles.Add(folderPath);
            UpdateFilesList();
        }
    }
    
    private void ClearSelection()
    {
        SelectedFiles.Clear();
        UpdateFilesList();
        extractedWords = new SeedingWordCollection([]);
        ProgressFraction = 0;
        ProgressText = "Ready";
        Statistics = "";
        CanSave = false;
        CanViewWords = false;
        CanExport = false;
    }
    
    private void UpdateFilesList()
    {
        FilesFrameTitle = $"Selected Files ({SelectedFiles.Count})";
        CanExtract = SelectedFiles.Count > 0 && !isExtracting;
    }
    
    private async Task ExtractWordsAsync()
    {
        if (SelectedFiles.Count == 0 || isExtracting)
        {
            return;
        }
        
        isExtracting = true;
        CanExtract = false;
        CanSave = false;
        CanViewWords = false;
        CanExport = false;
        
        try
        {
            var progress = new Progress<EpubExtractionProgress>(p =>
            {
                var fraction = p.TotalFiles > 0 ? (float)p.FilesProcessed / p.TotalFiles : 0;
                ProgressFraction = fraction;
                
                if (!string.IsNullOrEmpty(p.CurrentFile))
                {
                    ProgressText = $"Processing: {p.CurrentFile}";
                }
                else
                {
                    ProgressText = "Extraction complete";
                }
                
                UpdateStats(p.TotalWords, 0, 0, p.FilesProcessed, p.TotalFiles);
            });
            
            extractedWords = await seedingWordService.ExtractWordsFromFiles(
                SelectedFiles.ToArray(),
                progress);
            var existingWords = await seedingWordService.LoadSeedingWords();
            var newWordsCount = existingWords.GetNewWordsCount(extractedWords);
            
            
            UpdateStats(
                extractedWords.Count,
                newWordsCount,
                existingWords.Count,
                SelectedFiles.Count,
                SelectedFiles.Count);
            
            StatusMessage = $"Extraction complete! Found {extractedWords.Count} unique words";
            CanSave = true;
            CanViewWords = true;
            CanExport = true;
            
            ShowDialog(
                "Extraction Complete",
                $"Successfully extracted words from {SelectedFiles.Count} EPUB file(s).\n\n" +
                $"• Total unique words: {extractedWords.Count:N0}\n" +
                $"• New words: {newWordsCount:N0}\n" +
                $"• Already known: {existingWords.Count:N0}");
        }
        catch (Exception ex)
        {
            ShowDialog("Extraction Failed", $"Failed to extract words from EPUB files:\n\n{ex.Message}");
        }
        finally
        {
            isExtracting = false;
            CanExtract = SelectedFiles.Count > 0;
        }
    }
    
    private void UpdateStats(int totalWords, int newWords, int existingWords, int filesProcessed, int totalFiles)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Total words extracted: {totalWords:N0}");
        
        if (newWords > 0 || existingWords > 0)
        {
            sb.AppendLine($"New words: {newWords:N0} | Already known: {existingWords:N0}");
        }
        
        sb.AppendLine($"From {filesProcessed} / {totalFiles} files");
        
        Statistics = sb.ToString();
    }
    
    private async Task SaveToSeedingWordsAsync()
    {
        if (extractedWords.Count == 0)
        {
            return;
        }
        
        try
        {
            // Load existing and merge
            var existingWords = await seedingWordService.LoadSeedingWords();
            var fileName = await seedingWordService.GetSeedingWordsFileName();
            existingWords.AddWords(extractedWords.Words);
            await seedingWordService.SaveSeedingWords(existingWords);
            
            
            ShowDialog(
                "Saved to Seeding Words",
                $"Successfully saved words to seeding file.\n\n" +
                $"• Total words in file: {existingWords.Count:N0}\n" +
                $"• File: {fileName}");
        }
        catch (Exception ex)
        {
            ShowDialog("Save Failed", $"Failed to save words to seeding file:\n\n{ex.Message}");
        }
    }
    
    private void ViewWords()
    {
        if (extractedWords.Count == 0)
        {
            return;
        }
        
        var wordList = extractedWords
            .Words
            .OrderBy(w => w, StringComparer.Create(CultureInfo.CurrentCulture, true))
            .Take(1000)
            .ToList();
        
        var message = string.Join("\n", wordList);
        if (extractedWords.Count > 1000)
        {
            message += $"\n\n... and {extractedWords.Count - 1000} more words";
        }
        
        ViewWordsRequested?.Invoke(this, new ViewWordsEventArgs(
            $"Extracted Words (showing {Math.Min(1000, extractedWords.Count)} of {extractedWords.Count})",
            message));
    }
}

public class ViewWordsEventArgs : EventArgs
{
    public string Title { get; }
    public string Message { get; }
    
    public ViewWordsEventArgs(string title, string message)
    {
        Title = title;
        Message = message;
    }
}
