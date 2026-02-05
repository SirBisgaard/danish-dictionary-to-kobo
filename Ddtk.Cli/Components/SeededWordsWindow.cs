using System.Collections.ObjectModel;
using System.Text;
using Ddtk.Business;
using Ddtk.Domain;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class SeededWordsWindow : Window
{
    private readonly AppSettings appSettings;
    private readonly TextField searchField;
    private readonly ListView wordsListView;
    private readonly Label statsLabel;
    private readonly Label statusLabel;
    private List<string> allWords = [];
    private ObservableCollection<string> filteredWords = [];

    public SeededWordsWindow(MainMenuBar menu, MainStatusBar statusBar, AppSettings appSettings)
    {
        this.appSettings = appSettings;
        
        Title = string.Empty;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.None;
        Arrangement = ViewArrangement.Resizable;

        FrameView window = new()
        {
            Title = "Seeded Words Management",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        // Search section
        Label searchLabel = new()
        {
            Text = "Search:",
            X = 2,
            Y = 1
        };

        this.searchField = new TextField
        {
            X = Pos.Right(searchLabel) + 1,
            Y = 1,
            Width = 30
        };
        this.searchField.TextChanged += (s, e) => FilterWords();

        Button clearSearchButton = new()
        {
            Text = "Clear",
            X = Pos.Right(this.searchField) + 2,
            Y = 1
        };
        clearSearchButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            this.searchField.Text = string.Empty;
        };

        // List view frame
        FrameView listFrame = new()
        {
            Title = "Seeded Words (0 total)",
            X = 2,
            Y = 3,
            Width = Dim.Fill() - 4,
            Height = Dim.Fill() - 13
        };

        this.wordsListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        listFrame.Add(this.wordsListView);

        // Action buttons
        Button refreshButton = new()
        {
            Text = "Refresh",
            X = 2,
            Y = Pos.Bottom(listFrame) + 1
        };
        refreshButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            LoadWords();
        };

        Button exportButton = new()
        {
            Text = "Export to File",
            X = Pos.Right(refreshButton) + 2,
            Y = Pos.Bottom(listFrame) + 1
        };
        exportButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ExportToFile();
        };

        // Statistics section
        this.statsLabel = new Label
        {
            Text = "Statistics: Loading...",
            X = 2,
            Y = Pos.Bottom(listFrame) + 3,
            Width = Dim.Fill() - 4,
            Height = 3
        };

        // Status label
        this.statusLabel = new Label
        {
            Text = "Loading words...",
            X = 2,
            Y = Pos.AnchorEnd() - 1,
            Width = Dim.Fill() - 4
        };

        window.Add(
            searchLabel, this.searchField, clearSearchButton,
            listFrame,
            refreshButton, exportButton,
            this.statsLabel, this.statusLabel
        );
        
        Add(menu, window, statusBar);

        // Load words on initialization
        Task.Run(LoadWords);
    }

    private async void LoadWords()
    {
        try
        {
            this.statusLabel.Text = "Loading words...";
            
            var mediator = new ProcessMediator(this.appSettings);
            await using (mediator)
            {
                var words = await mediator.LoadSeedingWords();
                this.allWords = [.. words.OrderBy(w => w, StringComparer.Create(this.appSettings.Culture, true))];
                
                // Load statistics
                var wordDefinitions = await mediator.LoadWordDefinitionsJson();
                var processedSet = new HashSet<string>(
                    wordDefinitions.Select(wd => wd.Word.ToLowerInvariant()),
                    StringComparer.OrdinalIgnoreCase);
                
                var alreadyProcessed = this.allWords.Count(w => processedSet.Contains(w));
                var remaining = this.allWords.Count - alreadyProcessed;
                
                App?.Invoke(() =>
                {
                    FilterWords();
                    UpdateStats(this.allWords.Count, alreadyProcessed, remaining);
                    this.statusLabel.Text = $"Loaded {this.allWords.Count} words";
                });
            }
        }
        catch (Exception ex)
        {
            App?.Invoke(() =>
            {
                this.statusLabel.Text = $"Error loading words: {ex.Message}";
            });
        }
    }

    private void FilterWords()
    {
        var searchText = this.searchField.Text?.ToLowerInvariant() ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            this.filteredWords = new ObservableCollection<string>(this.allWords);
        }
        else
        {
            this.filteredWords = new ObservableCollection<string>(
                this.allWords.Where(w => w.ToLowerInvariant().Contains(searchText)));
        }
        
        this.wordsListView.SetSource(this.filteredWords);
        
        var parentFrame = this.wordsListView.SuperView as FrameView;
        if (parentFrame != null)
        {
            parentFrame.Title = $"Seeded Words ({this.allWords.Count} total | {this.filteredWords.Count} shown)";
        }
    }

    private void UpdateStats(int total, int processed, int remaining)
    {
        var percentage = total > 0 ? (double)processed / total * 100 : 0;
        var sb = new StringBuilder();
        sb.AppendLine("Statistics:");
        sb.AppendLine($"  • Total words: {total:N0}");
        sb.AppendLine($"  • Already processed: {processed:N0} ({percentage:F1}%)");
        sb.AppendLine($"  • Remaining to scrape: {remaining:N0}");
        
        this.statsLabel.Text = sb.ToString();
    }

    private void ExportToFile()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"exported_words_{timestamp}.txt";
            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            
            var content = string.Join(";", this.allWords);
            File.WriteAllText(filePath, content, Encoding.UTF8);
            
            this.statusLabel.Text = $"Exported {this.allWords.Count} word(s) to {fileName}";
        }
        catch (Exception ex)
        {
            this.statusLabel.Text = $"Error exporting: {ex.Message}";
        }
    }
}
