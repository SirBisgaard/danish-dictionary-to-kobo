using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;
using Ddtk.Business;
using Ddtk.Domain;
using ReactiveUI;

namespace Ddtk.Cli.ViewModels;

public class SeededWordsViewModel : ViewModelBase
{
    private readonly AppSettings appSettings;
    private readonly ProcessMediator processMediator;
    
    // Properties
    public string SearchText
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            FilterWords();
        }
    } = string.Empty;

    private List<string> allWords = [];

    public ObservableCollection<string> FilteredWords
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    public string ListFrameTitle
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Seeded Words (0 total)";

    public string Statistics
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Statistics: Loading...";

    // Commands
    public ReactiveCommand<Unit, Unit> LoadWordsCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearSearchCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }
    
    public SeededWordsViewModel(AppSettings appSettings, ProcessMediator processMediator)
    {
        this.appSettings = appSettings;
        this.processMediator = processMediator;
        
        LoadWordsCommand = ReactiveCommand.CreateFromTask(LoadWordsAsync);
        ClearSearchCommand = ReactiveCommand.Create(() => { SearchText = string.Empty; });
        ExportCommand = ReactiveCommand.Create(ExportToFile);
        
        // Auto-load words on construction
        LoadWordsCommand.Execute().Subscribe();
    }
    
    private async Task LoadWordsAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading words...";
            
            // Use injected processMediator
            // Mediator is now managed by DI
            {
                var words = await processMediator.LoadSeedingWords();
                allWords = [.. words.OrderBy(w => w, StringComparer.Create(appSettings.Culture, true))];
                
                // Load statistics
                var wordDefinitions = await processMediator.LoadWordDefinitionsJson();
                var processedSet = new HashSet<string>(
                    wordDefinitions.Select(wd => wd.Word.ToLowerInvariant()),
                    StringComparer.OrdinalIgnoreCase);
                
                var alreadyProcessed = allWords.Count(w => processedSet.Contains(w));
                var remaining = allWords.Count - alreadyProcessed;
                
                FilterWords();
                UpdateStats(allWords.Count, alreadyProcessed, remaining);
                StatusMessage = $"Loaded {allWords.Count} words";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading words: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void FilterWords()
    {
        var searchLower = SearchText?.ToLowerInvariant() ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(searchLower))
        {
            FilteredWords = new ObservableCollection<string>(allWords);
        }
        else
        {
            FilteredWords = new ObservableCollection<string>(
                allWords.Where(w => w.ToLowerInvariant().Contains(searchLower)));
        }
        
        ListFrameTitle = $"Seeded Words ({allWords.Count} total | {FilteredWords.Count} shown)";
    }
    
    private void UpdateStats(int total, int processed, int remaining)
    {
        var percentage = total > 0 ? (double)processed / total * 100 : 0;
        var sb = new StringBuilder();
        sb.AppendLine("Statistics:");
        sb.AppendLine($"  • Total words: {total:N0}");
        sb.AppendLine($"  • Already processed: {processed:N0} ({percentage:F1}%)");
        sb.AppendLine($"  • Remaining to scrape: {remaining:N0}");
        
        Statistics = sb.ToString();
    }
    
    private void ExportToFile()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"exported_words_{timestamp}.txt";
            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            
            var content = string.Join(";", allWords);
            File.WriteAllText(filePath, content, Encoding.UTF8);
            
            StatusMessage = $"Exported {allWords.Count} word(s) to {fileName}";
            
            ShowDialog(
                "Export Successful",
                $"Successfully exported {allWords.Count:N0} words to:\n\n{fileName}\n\n" +
                $"Location: {AppContext.BaseDirectory}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting: {ex.Message}";
            ShowDialog("Export Failed", $"Failed to export words:\n\n{ex.Message}");
        }
    }
}
