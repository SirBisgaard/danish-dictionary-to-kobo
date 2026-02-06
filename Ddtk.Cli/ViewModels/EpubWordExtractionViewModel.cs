using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using Ddtk.Business.Services;
using Ddtk.Domain;
using Ddtk.Domain.Models;
using ReactiveUI;

namespace Ddtk.Cli.ViewModels;

public class EpubWordExtractionViewModel : ViewModelBase
{
    private readonly AppSettings appSettings;
    
    // Properties
    private ObservableCollection<string> selectedFiles = [];
    public ObservableCollection<string> SelectedFiles
    {
        get => selectedFiles;
        set => this.RaiseAndSetIfChanged(ref selectedFiles, value);
    }
    
    private string filesFrameTitle = "Selected Files (0)";
    public string FilesFrameTitle
    {
        get => filesFrameTitle;
        set => this.RaiseAndSetIfChanged(ref filesFrameTitle, value);
    }
    
    private float progressFraction = 0f;
    public float ProgressFraction
    {
        get => progressFraction;
        set => this.RaiseAndSetIfChanged(ref progressFraction, value);
    }
    
    private string progressText = "Ready";
    public string ProgressText
    {
        get => progressText;
        set => this.RaiseAndSetIfChanged(ref progressText, value);
    }
    
    private string statistics = string.Empty;
    public string Statistics
    {
        get => statistics;
        set => this.RaiseAndSetIfChanged(ref statistics, value);
    }
    
    private bool canExtract = false;
    public bool CanExtract
    {
        get => canExtract;
        set => this.RaiseAndSetIfChanged(ref canExtract, value);
    }
    
    private bool canSave = false;
    public bool CanSave
    {
        get => canSave;
        set => this.RaiseAndSetIfChanged(ref canSave, value);
    }
    
    private bool canViewWords = false;
    public bool CanViewWords
    {
        get => canViewWords;
        set => this.RaiseAndSetIfChanged(ref canViewWords, value);
    }
    
    private bool canExport = false;
    public bool CanExport
    {
        get => canExport;
        set => this.RaiseAndSetIfChanged(ref canExport, value);
    }
    
    private HashSet<string> extractedWords = [];
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
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }
    
    public EpubWordExtractionViewModel(AppSettings appSettings)
    {
        this.appSettings = appSettings;
        
        SelectFilesCommand = ReactiveCommand.Create(() => SelectFilesRequested?.Invoke(this, EventArgs.Empty));
        SelectFolderCommand = ReactiveCommand.Create(() => SelectFolderRequested?.Invoke(this, EventArgs.Empty));
        ClearSelectionCommand = ReactiveCommand.Create(ClearSelection);
        ExtractWordsCommand = ReactiveCommand.CreateFromTask(ExtractWordsAsync, 
            this.WhenAnyValue(vm => vm.CanExtract));
        SaveToSeedingWordsCommand = ReactiveCommand.CreateFromTask(SaveToSeedingWordsAsync,
            this.WhenAnyValue(vm => vm.CanSave));
        ViewWordsCommand = ReactiveCommand.Create(ViewWords,
            this.WhenAnyValue(vm => vm.CanViewWords));
        ExportCommand = ReactiveCommand.CreateFromTask(ExportToFileAsync,
            this.WhenAnyValue(vm => vm.CanExport));
        
        StatusMessage = "Select EPUB files to extract words";
    }
    
    public void AddFiles(IEnumerable<string> filePaths)
    {
        var addedCount = 0;
        foreach (var filePath in filePaths)
        {
            if (filePath.EndsWith(".epub", StringComparison.OrdinalIgnoreCase) && 
                !SelectedFiles.Contains(filePath))
            {
                SelectedFiles.Add(filePath);
                addedCount++;
            }
        }
        
        UpdateFilesList();
        StatusMessage = $"Added {addedCount} file(s)";
    }
    
    public void AddFilesFromFolder(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            var epubFiles = Directory.GetFiles(folderPath, "*.epub", SearchOption.AllDirectories);
            var addedCount = 0;
            
            foreach (var epubFile in epubFiles)
            {
                if (!SelectedFiles.Contains(epubFile))
                {
                    SelectedFiles.Add(epubFile);
                    addedCount++;
                }
            }
            
            UpdateFilesList();
            StatusMessage = $"Added {addedCount} EPUB file(s) from folder";
        }
        else
        {
            // Treat as single file selection
            if (folderPath.EndsWith(".epub", StringComparison.OrdinalIgnoreCase) && 
                !SelectedFiles.Contains(folderPath))
            {
                SelectedFiles.Add(folderPath);
                UpdateFilesList();
                StatusMessage = "Added 1 file";
            }
        }
    }
    
    private void ClearSelection()
    {
        SelectedFiles.Clear();
        UpdateFilesList();
        extractedWords.Clear();
        ProgressFraction = 0;
        ProgressText = "Ready";
        Statistics = "";
        CanSave = false;
        CanViewWords = false;
        CanExport = false;
        StatusMessage = "Selection cleared";
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
        StatusMessage = "Extracting words...";
        
        try
        {
            var service = new EpubWordExtractorService(appSettings);
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
            
            extractedWords = await service.ExtractWordsFromEpubs(
                SelectedFiles,
                progress);
            
            // Load existing words and calculate statistics
            var existingFilePath = Path.Combine(AppContext.BaseDirectory, appSettings.SeedingWordsFileName);
            var (newWords, existingWords) = await service.MergeWithExisting(extractedWords, existingFilePath);
            
            UpdateStats(
                extractedWords.Count,
                newWords.Count,
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
                $"• New words: {newWords.Count:N0}\n" +
                $"• Already known: {existingWords.Count:N0}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
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
            StatusMessage = "No words to save";
            return;
        }
        
        try
        {
            var service = new EpubWordExtractorService(appSettings);
            var filePath = Path.Combine(AppContext.BaseDirectory, appSettings.SeedingWordsFileName);
            
            // Load existing and merge
            var existingWords = await service.LoadExistingWords(filePath);
            var combined = new HashSet<string>(existingWords, StringComparer.OrdinalIgnoreCase);
            
            foreach (var word in extractedWords)
            {
                combined.Add(word);
            }
            
            await service.SaveWords(combined, filePath);
            
            StatusMessage = $"Saved {combined.Count} words to seeding file";
            
            ShowDialog(
                "Saved to Seeding Words",
                $"Successfully saved words to seeding file.\n\n" +
                $"• Total words in file: {combined.Count:N0}\n" +
                $"• File: {appSettings.SeedingWordsFileName}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
            ShowDialog("Save Failed", $"Failed to save words to seeding file:\n\n{ex.Message}");
        }
    }
    
    private void ViewWords()
    {
        if (extractedWords.Count == 0)
        {
            StatusMessage = "No words extracted yet";
            return;
        }
        
        var wordList = extractedWords
            .OrderBy(w => w, StringComparer.Create(appSettings.Culture, true))
            .Take(100)
            .ToList();
        
        var message = string.Join("\n", wordList);
        if (extractedWords.Count > 100)
        {
            message += $"\n\n... and {extractedWords.Count - 100} more words";
        }
        
        ViewWordsRequested?.Invoke(this, new ViewWordsEventArgs(
            $"Extracted Words (showing {Math.Min(100, extractedWords.Count)} of {extractedWords.Count})",
            message));
    }
    
    private async Task ExportToFileAsync()
    {
        if (extractedWords.Count == 0)
        {
            StatusMessage = "No words to export";
            return;
        }
        
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"extracted_words_{timestamp}.txt";
            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            
            var service = new EpubWordExtractorService(appSettings);
            await service.SaveWords(extractedWords, filePath);
            
            StatusMessage = $"Exported {extractedWords.Count} words to {fileName}";
            
            ShowDialog(
                "Export Successful",
                $"Successfully exported {extractedWords.Count:N0} words to:\n\n{fileName}\n\n" +
                $"Location: {AppContext.BaseDirectory}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting: {ex.Message}";
            ShowDialog("Export Failed", $"Failed to export words:\n\n{ex.Message}");
        }
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
