using System.Text;
using Ddtk.Business.Services;
using Ddtk.Cli.Helpers;
using Ddtk.Cli.Services;
using Ddtk.Domain.Models;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components.Windows;

public class DashboardWindow(MainMenuBar mainMenu, MainStatusBar statusBar, DataStatusService dataStatusService, WindowDataHelper wdh, Action<WindowChange> changeWindow) : BaseWindow
{
    // Layout
    private Label pipelineStatusLabel = new();
    private Label seedingWordsStatsLabel = new();
    private Label scrapedDataStatsLabel = new();
    private Label dictionaryBuildStatsLabel = new();
    private bool isLoading;

    public override void InitializeLayout()
    {
        Title = string.Empty;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.None;
        Arrangement = ViewArrangement.Resizable;

        FrameView window = new()
        {
            Title = "Danish Dictionary to Kobo - Dashboard",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        // Section 3: Quick Actions
        FrameView quickActionsFrame = new()
        {
            Title = "Quick Actions",
            X = 2,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Auto() 
        };
        Button refreshButton = new()
        {
            Text = "Refresh Data",
            X = 1,
            Y = 1
        };
        refreshButton.Accepting += async (s, e) =>
        {
            e.Handled = true;
            await LoadDashboardData(true);
        };
        Button extractWordsButton = new()
        {
            Text = "Extract Words",
            X = Pos.Right(refreshButton) + 2,
            Y = 1
        };
        extractWordsButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            changeWindow(WindowChange.EpubWordExtractionWindow);
        };
        Button viewSeedingButton = new()
        {
            Text = "View Seeded Words",
            X = Pos.Right(extractWordsButton) + 2,
            Y = 1
        };
        viewSeedingButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            changeWindow(WindowChange.SeededWordsWindow);
        };
        Button startScrapingButton = new()
        {
            Text = "Start Scraping",
            X = Pos.Right(viewSeedingButton) + 2,
            Y = 1
        };
        startScrapingButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            changeWindow(WindowChange.WebScrapingWindow);
        };
        Button buildDictionaryButton = new()
        {
            Text = "Build Dictionary",
            X = Pos.Right(startScrapingButton) + 2,
            Y = 1
        };
        buildDictionaryButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            changeWindow(WindowChange.DictionaryBuildWindow);
        };
        quickActionsFrame.Add(
            refreshButton,
            extractWordsButton,
            viewSeedingButton,
            startScrapingButton,
            buildDictionaryButton
        );

        // Section 2: Pipeline Status
        FrameView pipelineFrame = new()
        {
            Title = "Pipeline Status",
            X = 2,
            Y = Pos.Bottom(quickActionsFrame) + 1,
            Width = Dim.Fill() - 2,
            Height = 5
        };

        pipelineStatusLabel = new Label
        {
            Text = "Loading pipeline status...",
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = 2
        };

        pipelineFrame.Add(pipelineStatusLabel);

        // Section 3: Statistics (3 panels side by side)
        // Panel A: Seeding Words
        FrameView seedingWordsFrame = new()
        {
            Title = "Seeding Words",
            X = 2,
            Y = Pos.Bottom(pipelineFrame) + 1,
            Width = Dim.Percent(33),
            Height = 9
        };
        seedingWordsStatsLabel = new Label
        {
            Text = "Loading...",
            X = 1,
            Y = 0,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill()
        };
        seedingWordsFrame.Add(seedingWordsStatsLabel);

        // Panel B: Scraped Data
        FrameView scrapedDataFrame = new()
        {
            Title = "Scraped Data",
            X = Pos.Right(seedingWordsFrame),
            Y = Pos.Bottom(pipelineFrame) + 1,
            Width = Dim.Percent(33),
            Height = 9
        };
        scrapedDataStatsLabel = new Label
        {
            Text = "Loading...",
            X = 1,
            Y = 0,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill()
        };
        scrapedDataFrame.Add(scrapedDataStatsLabel);

        // Panel C: Dictionary Build
        FrameView dictionaryBuildFrame = new()
        {
            Title = "Dictionary Build",
            X = Pos.Right(scrapedDataFrame),
            Y = Pos.Bottom(pipelineFrame) + 1,
            Width = Dim.Fill() - 2,
            Height = 9
        };
        dictionaryBuildStatsLabel = new Label
        {
            Text = "Loading...",
            X = 1,
            Y = 0,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill()
        };
        dictionaryBuildFrame.Add(dictionaryBuildStatsLabel);

        window.Add(
            quickActionsFrame,
            pipelineFrame,
            seedingWordsFrame,
            scrapedDataFrame,
            dictionaryBuildFrame
        );
        Add(mainMenu, window, statusBar);
    }

    public override void LoadData()
    {
        Task.Run(() => LoadDashboardData(false));
    }

    private async Task LoadDashboardData(bool showRefreshDialog)
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;

        App?.Invoke(() => { statusBar.SetStatus("Loading dashboard..."); });

        var dataStatus = await dataStatusService.GetCurrentStatus();

        App?.Invoke(() =>
        {
            UpdatePipelineStatus(dataStatus);
            UpdateSeedingWordsStats(dataStatus);
            UpdateScrapedDataStats(dataStatus);
            UpdateDictionaryBuildStats(dataStatus);

            statusBar.SetStatus($"Dashboard updated at {DateTime.Now:HH:mm:ss}.");

            if (showRefreshDialog)
            {
                DialogService.ShowDialog(
                    App,
                    "Dashboard Refreshed",
                    $"Successfully loaded dashboard data.\n\n" +
                    $"• Seeding words: {dataStatus.SeedingWordsCount:N0}\n" +
                    $"• Scraped definitions: {dataStatus.WordDefinitionCount:N0}\n" +
                    $"• Dictionary built: {(dataStatus.KoboDictionaryFileSize > 0 ? "Yes" : "No")}");
            }
        });

        isLoading = false;
    }

    private void UpdatePipelineStatus(DataStatus dataStatus)
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

        pipelineStatusLabel.Text = sb.ToString();
    }

    private void UpdateSeedingWordsStats(DataStatus dataStatus)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Status: " + (dataStatus.SeedingWordsFileSize > 0 ? "✓ Built" : "○ Not built"));
        sb.AppendLine($"Words: {dataStatus.SeedingWordsCount:N0}");
        sb.AppendLine($"Size: {wdh.ToFileSize(dataStatus.SeedingWordsFileSize)}");
        sb.AppendLine($"Last build: {wdh.ToDateTime(dataStatus.SeedingWordsFileChangeDate)}");
        sb.AppendLine();
        sb.AppendLine($"File: {dataStatus.SeedingFileName}");
        sb.AppendLine($"Updated: {wdh.ToTimeSince(dataStatus.SeedingWordsFileChangeDate, DateTime.Now)}");
        seedingWordsStatsLabel.Text = sb.ToString();
    }

    private void UpdateScrapedDataStats(DataStatus dataStatus)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Status: " + (dataStatus.WordDefinitionFileSize > 0 ? "✓ Built" : "○ Not built"));
        sb.AppendLine($"Definitions: {dataStatus.WordDefinitionCount:N0}");
        sb.AppendLine($"Size: {wdh.ToFileSize(dataStatus.WordDefinitionFileSize)}");
        sb.AppendLine($"Last build: {wdh.ToDateTime(dataStatus.WordDefinitionFileChangeDate)}");
        sb.AppendLine();
        sb.AppendLine($"File: {dataStatus.WordDefinitionFileName}");
        sb.AppendLine($"Updated: {wdh.ToTimeSince(DateTime.Now, dataStatus.WordDefinitionFileChangeDate)}");
        scrapedDataStatsLabel.Text = sb.ToString();
    }

    private void UpdateDictionaryBuildStats(DataStatus dataStatus)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Status: " + (dataStatus.KoboDictionaryFileSize > 0 ? "✓ Built" : "○ Not built"));
        sb.AppendLine($"Size: {wdh.ToFileSize(dataStatus.KoboDictionaryFileSize)}");
        sb.AppendLine($"Last build: {wdh.ToDateTime(dataStatus.KoboDictionaryFileChangeDate)}");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine($"File: {dataStatus.KoboDictionaryFileName}");
        sb.AppendLine($"Updated: {wdh.ToTimeSince(DateTime.Now, dataStatus.KoboDictionaryFileChangeDate)}");
        dictionaryBuildStatsLabel.Text = sb.ToString();
    }
}