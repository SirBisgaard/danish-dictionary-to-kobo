using System.Reactive;
using System.Text;
using Ddtk.Business.Services;
using Ddtk.Cli.Helpers;
using Ddtk.Cli.Interfaces;
using Ddtk.Cli.Services;
using Ddtk.Domain.Models;
using ReactiveUI;

namespace Ddtk.Cli.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly DataStatusService dataStatusService;
    private readonly WindowDataHelper windowDataHelper;
    private readonly INavigationService navigationService;
    
    // Observable properties for UI binding
    public string PipelineStatus
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Loading...";

    public string SeedingWordsStats
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Loading...";

    public string ScrapedDataStats
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Loading...";

    public string DictionaryBuildStats
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Loading...";

    public bool IsLoading
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractWordsCommand { get; }
    public ReactiveCommand<Unit, Unit> ViewSeedingCommand { get; }
    public ReactiveCommand<Unit, Unit> StartScrapingCommand { get; }
    public ReactiveCommand<Unit, Unit> BuildDictionaryCommand { get; }
    
    public DashboardViewModel(
        DataStatusService dataStatusService,
        WindowDataHelper windowDataHelper,
        INavigationService navigationService)
    {
        this.dataStatusService = dataStatusService;
        this.windowDataHelper = windowDataHelper;
        this.navigationService = navigationService;
        
        // Initialize commands
        RefreshCommand = ReactiveCommand.CreateFromTask(async () => await LoadDashboardDataAsync(true));
        
        ExtractWordsCommand = ReactiveCommand.Create(() =>
        {
            navigationService.NavigateTo(WindowChange.EpubWordExtractionWindow);
        });
        
        ViewSeedingCommand = ReactiveCommand.Create(() =>
        {
            navigationService.NavigateTo(WindowChange.SeededWordsWindow);
        });
        
        StartScrapingCommand = ReactiveCommand.Create(() =>
        {
            navigationService.NavigateTo(WindowChange.WebScrapingWindow);
        });
        
        BuildDictionaryCommand = ReactiveCommand.Create(() =>
        {
            navigationService.NavigateTo(WindowChange.DictionaryBuildWindow);
        });
    }
    
    public async Task LoadDashboardDataAsync(bool showRefreshDialog)
    {
        if (IsLoading)
        {
            return;
        }
        
        IsLoading = true;
        StatusMessage = "Loading dashboard...";
        
        try
        {
            var dataStatus = await dataStatusService.GetCurrentStatus();
            
            // Update all display properties
            PipelineStatus = FormatPipelineStatus(dataStatus);
            SeedingWordsStats = FormatSeedingWordsStats(dataStatus);
            ScrapedDataStats = FormatScrapedDataStats(dataStatus);
            DictionaryBuildStats = FormatDictionaryBuildStats(dataStatus);
            
            StatusMessage = $"Dashboard updated at {DateTime.Now:HH:mm:ss}.";
            
            if (showRefreshDialog)
            {
                ShowDialog(
                    "Dashboard Refreshed",
                    $"Successfully loaded dashboard data.\n\n" +
                    $"• Seeding words: {dataStatus.SeedingWordsCount:N0}\n" +
                    $"• Scraped definitions: {dataStatus.WordDefinitionCount:N0}\n" +
                    $"• Dictionary built: {(dataStatus.KoboDictionaryFileSize > 0 ? "Yes" : "No")}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            ShowDialog("Error", $"Failed to load dashboard data:\n\n{ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private string FormatPipelineStatus(DataStatus dataStatus)
    {
        var sb = new StringBuilder();
        
        // Step 1: Extract (check if we have seeding words)
        var seedingStatus = dataStatus.SeedingWordsCount > 0 ? "✓" : "○";
        var seedingText = dataStatus.SeedingWordsCount > 0 ? "Ready" : "Pending";
        
        // Step 2: Scrape (check progress)
        var scrapeStatus = dataStatus.WordDefinitionCount > 0 ? "✓" : "○";
        var scrapeText = dataStatus.SeedingWordsCount > 0 ? "Ready" : "Pending";
        
        // Step 3: Process (same as scrape for now)
        var processStatus = dataStatus.WordDefinitionCount > 0 ? "✓" : "○";
        var processText = dataStatus.SeedingWordsCount > 0 ? "Ready" : "Pending";
        
        // Step 4: Build (check if dictionary exists)
        var buildStatus = dataStatus.KoboDictionaryFileSize > 0 ? "✓" : "○";
        var buildText = dataStatus.KoboDictionaryFileSize > 0 ? "Complete" : "Pending";
        
        sb.AppendLine($"  Seeding - {seedingStatus} {seedingText}  →  | Scraping - {scrapeStatus} {scrapeText}  →  | Process - {processStatus} {processText}  →  | Build - {buildStatus} {buildText}");
        
        return sb.ToString();
    }
    
    private string FormatSeedingWordsStats(DataStatus dataStatus)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Status: " + (dataStatus.SeedingWordsFileSize > 0 ? "✓ Built" : "○ Not built"));
        sb.AppendLine($"Words: {dataStatus.SeedingWordsCount:N0}");
        sb.AppendLine($"Size: {windowDataHelper.ToFileSize(dataStatus.SeedingWordsFileSize)}");
        sb.AppendLine($"Last build: {windowDataHelper.ToDateTime(dataStatus.SeedingWordsFileChangeDate)}");
        sb.AppendLine();
        sb.AppendLine($"File: {dataStatus.SeedingFileName}");
        sb.AppendLine($"Updated: {windowDataHelper.ToTimeSince(dataStatus.SeedingWordsFileChangeDate, DateTime.Now)}");
        return sb.ToString();
    }
    
    private string FormatScrapedDataStats(DataStatus dataStatus)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Status: " + (dataStatus.WordDefinitionFileSize > 0 ? "✓ Built" : "○ Not built"));
        sb.AppendLine($"Definitions: {dataStatus.WordDefinitionCount:N0}");
        sb.AppendLine($"Size: {windowDataHelper.ToFileSize(dataStatus.WordDefinitionFileSize)}");
        sb.AppendLine($"Last build: {windowDataHelper.ToDateTime(dataStatus.WordDefinitionFileChangeDate)}");
        sb.AppendLine();
        sb.AppendLine($"File: {dataStatus.WordDefinitionFileName}");
        sb.AppendLine($"Updated: {windowDataHelper.ToTimeSince(DateTime.Now, dataStatus.WordDefinitionFileChangeDate)}");
        return sb.ToString();
    }
    
    private string FormatDictionaryBuildStats(DataStatus dataStatus)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Status: " + (dataStatus.KoboDictionaryFileSize > 0 ? "✓ Built" : "○ Not built"));
        sb.AppendLine($"Size: {windowDataHelper.ToFileSize(dataStatus.KoboDictionaryFileSize)}");
        sb.AppendLine($"Last build: {windowDataHelper.ToDateTime(dataStatus.KoboDictionaryFileChangeDate)}");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine($"File: {dataStatus.KoboDictionaryFileName}");
        sb.AppendLine($"Updated: {windowDataHelper.ToTimeSince(DateTime.Now, dataStatus.KoboDictionaryFileChangeDate)}");
        return sb.ToString();
    }
}
