using Ddtk.Business;
using Ddtk.Cli.Services;
using Ddtk.Domain;
using Ddtk.Domain.Models;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components.Windows;

public class WebScrapingWindow : BaseWindow
{
    private readonly AppSettings appSettings;
    private readonly TextField workerCountField;
    private readonly Button startButton;
    private readonly Button stopButton;
    private readonly ProgressBar progressBar;
    private readonly Label progressPercentLabel;
    private readonly Label wordsScrapedLabel;
    private readonly Label queueSizeLabel;
    private readonly Label elapsedTimeLabel;
    private readonly TextView activityLogView;
    private readonly Button saveResultsButton;
    private readonly Label statusLabel;
    private CancellationTokenSource? cancellationTokenSource;
    private bool isScraping;
    private List<string> activityLog = [];

    public WebScrapingWindow(MainMenuBar menu, MainStatusBar statusBar, AppSettings appSettings)
    {
        this.appSettings = appSettings;
        
        Title = string.Empty;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.None;
        Arrangement = ViewArrangement.Resizable;

        FrameView window = new()
        {
            Title = "Web Scraping",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        // Configuration section
        FrameView configFrame = new()
        {
            Title = "Configuration",
            X = 2,
            Y = 1,
            Width = Dim.Fill() - 4,
            Height = 5
        };

        Label configInfoLabel = new()
        {
            Text = "Configuration: Using seeded words with default settings",
            X = 1,
            Y = 0
        };

        Label workerCountLabel = new()
        {
            Text = "Worker count:",
            X = 1,
            Y = 2
        };

        workerCountField = new TextField
        {
            Text = this.appSettings.WebScraperWorkerCount.ToString(),
            X = Pos.Right(workerCountLabel) + 1,
            Y = 2,
            Width = 10
        };

        configFrame.Add(
            configInfoLabel,
            workerCountLabel,
            workerCountField
        );

        // Control buttons
        startButton = new Button
        {
            Text = "Start Scraping",
            X = 2,
            Y = Pos.Bottom(configFrame) + 1
        };
        startButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            StartScraping();
        };

        stopButton = new Button
        {
            Text = "Stop",
            X = Pos.Right(startButton) + 2,
            Y = Pos.Bottom(configFrame) + 1,
            Enabled = false
        };
        stopButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            StopScraping();
        };

        // Progress section
        Label progressLabel = new()
        {
            Text = "Progress:",
            X = 2,
            Y = Pos.Bottom(configFrame) + 3
        };

        progressBar = new ProgressBar
        {
            X = Pos.Right(progressLabel) + 1,
            Y = Pos.Bottom(configFrame) + 3,
            Width = Dim.Fill() - 4 - progressLabel.Text.Length - 10,
            Height = 1
        };

        progressPercentLabel = new Label
        {
            Text = "0%",
            X = Pos.Right(progressBar) + 1,
            Y = Pos.Bottom(configFrame) + 3,
            Width = 8
        };

        // Status frame
        FrameView statusFrame = new()
        {
            Title = "Status",
            X = 2,
            Y = Pos.Bottom(configFrame) + 4,
            Width = Dim.Fill() - 4,
            Height = 5
        };

        wordsScrapedLabel = new Label
        {
            Text = "Words scraped: 0 / 0",
            X = 1,
            Y = 0
        };

        queueSizeLabel = new Label
        {
            Text = "Queue size: 0",
            X = 1,
            Y = 1
        };

        elapsedTimeLabel = new Label
        {
            Text = "Elapsed time: 00:00:00",
            X = 1,
            Y = 2
        };

        statusFrame.Add(wordsScrapedLabel, queueSizeLabel, elapsedTimeLabel);

        // Activity log
        FrameView logFrame = new()
        {
            Title = "Activity Log",
            X = 2,
            Y = Pos.Bottom(statusFrame) + 1,
            Width = Dim.Fill() - 4,
            Height = 10
        };

        activityLogView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = false
        };

        logFrame.Add(activityLogView);

        // Action buttons
        saveResultsButton = new Button
        {
            Text = "Save Results to JSON",
            X = 2,
            Y = Pos.Bottom(logFrame) + 1,
            Enabled = false
        };
        saveResultsButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            SaveResults();
        };

        Button viewResultsButton = new()
        {
            Text = "View Results",
            X = Pos.Right(saveResultsButton) + 2,
            Y = Pos.Bottom(logFrame) + 1,
            Enabled = false
        };
        viewResultsButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewResults();
        };

        // Status label
        statusLabel = new Label
        {
            Text = "Ready to start scraping",
            X = 2,
            Y = Pos.AnchorEnd() - 1,
            Width = Dim.Fill() - 4
        };

        window.Add(
            configFrame,
            startButton, stopButton,
            progressLabel, progressBar, progressPercentLabel,
            statusFrame,
            logFrame,
            saveResultsButton, viewResultsButton,
            statusLabel
        );
        
        Add(menu, window, statusBar);
    }

    private async void StartScraping()
    {
        if (isScraping)
        {
            return;
        }

        // Validate worker count
        if (!int.TryParse(workerCountField.Text, out int workerCount) || workerCount <= 0)
        {
            statusLabel.Text = "Error: Worker count must be a positive integer";
            return;
        }

        isScraping = true;
        startButton.Enabled = false;
        stopButton.Enabled = true;
        saveResultsButton.Enabled = false;
        cancellationTokenSource = new CancellationTokenSource();
        activityLog.Clear();

        // Create scraping options (with default runtime settings)
        var options = new ScrapingOptions
        {
            UseSeededWords = true,
            SaveHtmlFiles = false,
            UpdateExistingHtmlFiles = false,
            UpdateSeededWords = false,
            WorkerCount = workerCount
        };

        AddLog($"[{DateTime.Now:HH:mm:ss}] Starting web scraping...");
        AddLog($"[{DateTime.Now:HH:mm:ss}] Worker count: {options.WorkerCount}");

        try
        {
            var mediator = new ProcessMediator(appSettings);
            await using (mediator)
            {
                // Load seeding words
                string[] seedingWords;
                if (options.UseSeededWords)
                {
                    seedingWords = await mediator.LoadSeedingWords();
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Loaded {seedingWords.Length} seeding words");
                }
                else
                {
                    seedingWords = [];
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Starting with empty word list");
                }

                // Create progress reporter
                var progress = new Progress<ScrapingProgress>(p =>
                {
                    App?.Invoke(() =>
                    {
                        UpdateProgress(p);
                    });
                });

                // Start scraping
                var results = await mediator.RunScraping(
                    seedingWords,
                    options,
                    progress,
                    cancellationTokenSource.Token);

                App?.Invoke(() =>
                {
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Scraping completed!");
                    AddLog($"[{DateTime.Now:HH:mm:ss}] Total words scraped: {results.Count}");
                    statusLabel.Text = $"Scraping complete! {results.Count} words scraped";
                    saveResultsButton.Enabled = true;
                    
                    DialogService.ShowDialog(
                        App,
                        "Scraping Complete",
                        $"Successfully scraped word definitions from ordnet.dk.\n\n" +
                        $"• Total definitions scraped: {results.Count:N0}\n" +
                        $"• Saved to: {appSettings.WordDefinitionFile}");
                });
            }
        }
        catch (OperationCanceledException)
        {
            App?.Invoke(() =>
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] Scraping stopped by user");
                statusLabel.Text = "Scraping stopped";
                
                DialogService.ShowDialog(
                    App,
                    "Scraping Stopped",
                    "Web scraping was stopped by user.\n\nPartial results have been saved.");
            });
        }
        catch (Exception ex)
        {
            App?.Invoke(() =>
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}");
                statusLabel.Text = $"Error: {ex.Message}";
                
                DialogService.ShowDialog(
                    App,
                    "Scraping Failed",
                    $"An error occurred during web scraping:\n\n{ex.Message}");
            });
        }
        finally
        {
            isScraping = false;
            App?.Invoke(() =>
            {
                startButton.Enabled = true;
                stopButton.Enabled = false;
            });
        }
    }

    private void StopScraping()
    {
        if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Stopping scraping...");
            cancellationTokenSource.Cancel();
            statusLabel.Text = "Stopping...";
        }
    }

    private void UpdateProgress(ScrapingProgress progress)
    {
        // Update progress bar
        var fraction = progress.TotalWords > 0 ? (float)progress.WordsScraped / progress.TotalWords : 0;
        progressBar.Fraction = Math.Min(fraction, 1.0f);
        progressPercentLabel.Text = $"{progress.PercentComplete:F1}%";

        // Update status labels
        wordsScrapedLabel.Text = $"Words scraped: {progress.WordsScraped} / {progress.TotalWords}";
        queueSizeLabel.Text = $"Queue size: {progress.QueueSize}";
        elapsedTimeLabel.Text = $"Elapsed time: {progress.Elapsed:hh\\:mm\\:ss}";

        // Add log messages
        if (!string.IsNullOrEmpty(progress.LogMessage))
        {
            AddLog(progress.LogMessage);
        }
    }

    private void AddLog(string message)
    {
        activityLog.Add(message);
        
        // Keep last 1000 entries
        if (activityLog.Count > 1000)
        {
            activityLog.RemoveAt(0);
        }

        // Update text view
        var logText = string.Join("\n", activityLog);
        activityLogView.Text = logText;

        // Auto-scroll to bottom by moving cursor to end
        activityLogView.MoveEnd();
    }

    private void SaveResults()
    {
        statusLabel.Text = "Results are automatically saved to JSON during scraping";
    }

    private void ViewResults()
    {
        statusLabel.Text = "Navigate to 'Build Dictionary' window to process results";
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
