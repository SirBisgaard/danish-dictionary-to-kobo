using System.Collections.ObjectModel;
using System.Reactive;
using Ddtk.Business;
using Ddtk.Domain;
using Ddtk.Domain.Models;
using ReactiveUI;

namespace Ddtk.Cli.ViewModels;

public class WebScrapingViewModel : ViewModelBase, IDisposable
{
    private readonly AppSettings appSettings;
    private readonly ProcessMediator processMediator;
    private CancellationTokenSource? cancellationTokenSource;
    
    // Observable properties
    public int WorkerCount
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public float ProgressFraction
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string ProgressPercent
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "0%";

    public string WordsScraped
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Words scraped: 0 / 0";

    public string QueueSize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Queue size: 0";

    public string ElapsedTime
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Elapsed time: 00:00:00";

    public bool IsScraping
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool CanStart
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public bool CanStop
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool SaveResultsEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ObservableCollection<string> ActivityLog { get; } = [];
    
    // Commands
    public ReactiveCommand<Unit, Unit> StartScrapingCommand { get; }
    public ReactiveCommand<Unit, Unit> StopScrapingCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveResultsCommand { get; }
    
    public WebScrapingViewModel(AppSettings appSettings, ProcessMediator processMediator)
    {
        this.appSettings = appSettings;
        this.processMediator = processMediator;
        
        // Initialize worker count from settings
        WorkerCount = appSettings.WebScraperWorkerCount;
        
        // Initialize commands
        StartScrapingCommand = ReactiveCommand.CreateFromTask(
            StartScrapingAsync,
            this.WhenAnyValue(vm => vm.CanStart)
        );
        
        StopScrapingCommand = ReactiveCommand.CreateFromTask(
            StopScrapingAsync,
            this.WhenAnyValue(vm => vm.CanStop)
        );
        
        SaveResultsCommand = ReactiveCommand.Create(
            () =>
            {
                StatusMessage = "Results are automatically saved to JSON during scraping";
            },
            this.WhenAnyValue(vm => vm.SaveResultsEnabled)
        );
        
        // Initial status
        StatusMessage = "Ready to start scraping";
    }
    
    private async Task StartScrapingAsync()
    {
        if (IsScraping)
        {
            return;
        }
        
        // Validate worker count
        if (WorkerCount <= 0)
        {
            ShowDialog("Invalid Input", "Worker count must be a positive integer");
            return;
        }
        
        IsScraping = true;
        CanStart = false;
        CanStop = true;
        SaveResultsEnabled = false;
        cancellationTokenSource = new CancellationTokenSource();
        ActivityLog.Clear();
        
        // Create scraping options
        var options = new ScrapingOptions
        {
            UseSeededWords = true,
            SaveHtmlFiles = false,
            UpdateExistingHtmlFiles = false,
            UpdateSeededWords = false,
            WorkerCount = WorkerCount
        };
        
        AddLog($"[{DateTime.Now:HH:mm:ss}] Starting web scraping...");
        AddLog($"[{DateTime.Now:HH:mm:ss}] Worker count: {options.WorkerCount}");
        
        try
        {
            // Load seeding words
            string[] seedingWords;
            if (options.UseSeededWords)
            {
                seedingWords = await processMediator.LoadSeedingWords();
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
                UpdateProgress(p);
            });
            
            // Start scraping
            var results = await processMediator.RunScraping(
                seedingWords,
                options,
                progress,
                cancellationTokenSource.Token);
            
            AddLog($"[{DateTime.Now:HH:mm:ss}] Scraping completed!");
            AddLog($"[{DateTime.Now:HH:mm:ss}] Total words scraped: {results.Count}");
            StatusMessage = $"Scraping complete! {results.Count} words scraped";
            SaveResultsEnabled = true;
            
            ShowDialog(
                "Scraping Complete",
                $"Successfully scraped word definitions from ordnet.dk.\n\n" +
                $"• Total definitions scraped: {results.Count:N0}\n" +
                $"• Saved to: {appSettings.WordDefinitionFileName}");
        }
        catch (OperationCanceledException)
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Scraping stopped by user");
            StatusMessage = "Scraping stopped";
            
            ShowDialog(
                "Scraping Stopped",
                "Web scraping was stopped by user.\n\nPartial results have been saved.");
        }
        catch (Exception ex)
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}");
            StatusMessage = $"Error: {ex.Message}";
            
            ShowDialog(
                "Scraping Failed",
                $"An error occurred during web scraping:\n\n{ex.Message}");
        }
        finally
        {
            IsScraping = false;
            CanStart = true;
            CanStop = false;
        }
    }
    
    private async Task StopScrapingAsync()
    {
        if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] Stopping scraping...");
            cancellationTokenSource.Cancel();
            StatusMessage = "Stopping...";
        }
        
        await Task.CompletedTask;
    }
    
    private void UpdateProgress(ScrapingProgress progress)
    {
        // Update progress bar
        var fraction = progress.TotalWords > 0 ? (float)progress.WordsScraped / progress.TotalWords : 0;
        ProgressFraction = Math.Min(fraction, 1.0f);
        ProgressPercent = $"{progress.PercentComplete:F1}%";
        
        // Update status labels
        WordsScraped = $"Words scraped: {progress.WordsScraped} / {progress.TotalWords}";
        QueueSize = $"Queue size: {progress.QueueSize}";
        ElapsedTime = $"Elapsed time: {progress.Elapsed:hh\\:mm\\:ss}";
        
        // Add log messages
        if (!string.IsNullOrEmpty(progress.LogMessage))
        {
            AddLog(progress.LogMessage);
        }
    }
    
    private void AddLog(string message)
    {
        ActivityLog.Add(message);
        
        // Keep last 1000 entries
        while (ActivityLog.Count > 1000)
        {
            ActivityLog.RemoveAt(0);
        }
    }
    
    public void Dispose()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}
