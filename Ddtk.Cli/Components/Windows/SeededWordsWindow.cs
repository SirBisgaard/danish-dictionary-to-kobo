using System.Collections.ObjectModel;
using System.Text;
using Ddtk.Business;
using Ddtk.Cli.Services;
using Ddtk.Domain;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components.Windows;

public class SeededWordsWindow : BaseWindow
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

        searchField = new TextField
        {
            X = Pos.Right(searchLabel) + 1,
            Y = 1,
            Width = 30
        };
        searchField.TextChanged += (s, e) => FilterWords();

        Button clearSearchButton = new()
        {
            Text = "Clear",
            X = Pos.Right(searchField) + 2,
            Y = 1
        };
        clearSearchButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            searchField.Text = string.Empty;
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

        wordsListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        listFrame.Add(wordsListView);

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
        statsLabel = new Label
        {
            Text = "Statistics: Loading...",
            X = 2,
            Y = Pos.Bottom(listFrame) + 3,
            Width = Dim.Fill() - 4,
            Height = 3
        };

        // Status label
        statusLabel = new Label
        {
            Text = "Loading words...",
            X = 2,
            Y = Pos.AnchorEnd() - 1,
            Width = Dim.Fill() - 4
        };

        window.Add(
            searchLabel, searchField, clearSearchButton,
            listFrame,
            refreshButton, exportButton,
            statsLabel, statusLabel
        );
        
        Add(menu, window, statusBar);

        // Load words on initialization
        Task.Run(LoadWords);
    }

    private async void LoadWords()
    {
        try
        {
            statusLabel.Text = "Loading words...";
            
            var mediator = new ProcessMediator(appSettings);
            await using (mediator)
            {
                var words = await mediator.LoadSeedingWords();
                allWords = [.. words.OrderBy(w => w, StringComparer.Create(appSettings.Culture, true))];
                
                // Load statistics
                var wordDefinitions = await mediator.LoadWordDefinitionsJson();
                var processedSet = new HashSet<string>(
                    wordDefinitions.Select(wd => wd.Word.ToLowerInvariant()),
                    StringComparer.OrdinalIgnoreCase);
                
                var alreadyProcessed = allWords.Count(w => processedSet.Contains(w));
                var remaining = allWords.Count - alreadyProcessed;
                
                App?.Invoke(() =>
                {
                    FilterWords();
                    UpdateStats(allWords.Count, alreadyProcessed, remaining);
                    statusLabel.Text = $"Loaded {allWords.Count} words";
                });
            }
        }
        catch (Exception ex)
        {
            App?.Invoke(() =>
            {
                statusLabel.Text = $"Error loading words: {ex.Message}";
            });
        }
    }

    private void FilterWords()
    {
        var searchText = searchField.Text?.ToLowerInvariant() ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            filteredWords = new ObservableCollection<string>(allWords);
        }
        else
        {
            filteredWords = new ObservableCollection<string>(
                allWords.Where(w => w.ToLowerInvariant().Contains(searchText)));
        }
        
        wordsListView.SetSource(filteredWords);
        
        var parentFrame = wordsListView.SuperView as FrameView;
        if (parentFrame != null)
        {
            parentFrame.Title = $"Seeded Words ({allWords.Count} total | {filteredWords.Count} shown)";
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
        
        statsLabel.Text = sb.ToString();
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
            
            statusLabel.Text = $"Exported {allWords.Count} word(s) to {fileName}";
            
            DialogService.ShowDialog(
                App,
                "Export Successful",
                $"Successfully exported {allWords.Count:N0} words to:\n\n{fileName}\n\n" +
                $"Location: {AppContext.BaseDirectory}");
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Error exporting: {ex.Message}";
            DialogService.ShowDialog(
                App,
                "Export Failed",
                $"Failed to export words:\n\n{ex.Message}");
        }
    }

    public override void InitializeLayout()
    {
        throw new NotImplementedException();
    }

    public override void LoadData()
    {
        throw new NotImplementedException();
    }
}
