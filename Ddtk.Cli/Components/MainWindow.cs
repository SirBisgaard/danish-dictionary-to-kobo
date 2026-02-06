using System.Text;
using Ddtk.Business;
using Ddtk.Domain;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class MainWindow : Window
{
    private readonly AppSettings appSettings;
    private readonly Action<WindowChange> changeWindow;
    private readonly Label pipelineStatusLabel;
    private readonly Label seedingWordsStatsLabel;
    private readonly Label scrapedDataStatsLabel;
    private readonly Label dictionaryBuildStatsLabel;
    private readonly Label fileStatusLabel;
    private readonly Label statusLabel;
    private bool isLoading;

    // Statistics cache
    private int totalSeedingWords;
    private int scrapedWordsCount;
    private int remainingWords;
    private double progressPercent;
    private long jsonFileSize;
    private DateTime? lastScrapedDate;
    private bool dictionaryExists;
    private long dictionaryFileSize;
    private DateTime? lastBuildDate;

    public MainWindow(MainMenuBar menu, MainStatusBar statusBar, AppSettings appSettings, Action<WindowChange> changeWindow)
    {
        this.appSettings = appSettings;
        this.changeWindow = changeWindow;
        
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

        // Section 1: Pipeline Status
        FrameView pipelineFrame = new()
        {
            Title = "Pipeline Status",
            X = 2,
            Y = 1,
            Width = Dim.Fill() - 4,
            Height = 5
        };

        this.pipelineStatusLabel = new Label
        {
            Text = "Loading pipeline status...",
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = 2
        };

        pipelineFrame.Add(this.pipelineStatusLabel);

        // Section 2: Statistics (3 panels side by side)
        // Panel A: Seeding Words
        FrameView seedingWordsFrame = new()
        {
            Title = "Seeding Words",
            X = 2,
            Y = Pos.Bottom(pipelineFrame) + 1,
            Width = Dim.Percent(33),
            Height = 8
        };

        this.seedingWordsStatsLabel = new Label
        {
            Text = "Loading...",
            X = 1,
            Y = 0,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill()
        };

        seedingWordsFrame.Add(this.seedingWordsStatsLabel);

        // Panel B: Scraped Data
        FrameView scrapedDataFrame = new()
        {
            Title = "Scraped Data",
            X = Pos.Right(seedingWordsFrame),
            Y = Pos.Bottom(pipelineFrame) + 1,
            Width = Dim.Percent(33),
            Height = 8
        };

        this.scrapedDataStatsLabel = new Label
        {
            Text = "Loading...",
            X = 1,
            Y = 0,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill()
        };

        scrapedDataFrame.Add(this.scrapedDataStatsLabel);

        // Panel C: Dictionary Build
        FrameView dictionaryBuildFrame = new()
        {
            Title = "Dictionary Build",
            X = Pos.Right(scrapedDataFrame),
            Y = Pos.Bottom(pipelineFrame) + 1,
            Width = Dim.Percent(34),
            Height = 8
        };

        this.dictionaryBuildStatsLabel = new Label
        {
            Text = "Loading...",
            X = 1,
            Y = 0,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill()
        };

        dictionaryBuildFrame.Add(this.dictionaryBuildStatsLabel);

        // Section 3: Quick Actions
        FrameView quickActionsFrame = new()
        {
            Title = "Quick Actions",
            X = 2,
            Y = Pos.Bottom(seedingWordsFrame) + 1,
            Width = Dim.Fill() - 4,
            Height = 4
        };

        Button refreshButton = new()
        {
            Text = "Refresh Data",
            X = 1,
            Y = 1
        };
        refreshButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            LoadDashboardData();
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
            this.changeWindow(WindowChange.EpubWordExtractionWindow);
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
            this.changeWindow(WindowChange.SeededWordsWindow);
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
            this.changeWindow(WindowChange.WebScrapingWindow);
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
            this.changeWindow(WindowChange.DictionaryBuildWindow);
        };

        quickActionsFrame.Add(
            refreshButton,
            extractWordsButton,
            viewSeedingButton,
            startScrapingButton,
            buildDictionaryButton
        );

        // Section 4: File Status
        FrameView fileStatusFrame = new()
        {
            Title = "Files",
            X = 2,
            Y = Pos.Bottom(quickActionsFrame) + 1,
            Width = Dim.Fill() - 4,
            Height = 6
        };

        this.fileStatusLabel = new Label
        {
            Text = "Loading file status...",
            X = 1,
            Y = 0,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill()
        };

        fileStatusFrame.Add(this.fileStatusLabel);

        // Status label at bottom
        this.statusLabel = new Label
        {
            Text = "Loading dashboard...",
            X = 2,
            Y = Pos.AnchorEnd() - 1,
            Width = Dim.Fill() - 4
        };

        window.Add(
            pipelineFrame,
            seedingWordsFrame,
            scrapedDataFrame,
            dictionaryBuildFrame,
            quickActionsFrame,
            fileStatusFrame,
            this.statusLabel
        );
        
        Add(menu, window, statusBar);

        // Load data on initialization
        Task.Run(LoadDashboardData);
    }

    private async void LoadDashboardData()
    {
        if (this.isLoading)
        {
            return;
        }

        this.isLoading = true;
        App?.Invoke(() =>
        {
            this.statusLabel.Text = "Loading dashboard data...";
        });

        try
        {
            var mediator = new ProcessMediator(this.appSettings);
            await using (mediator)
            {
                // Load seeding words
                var seedingWords = await mediator.LoadSeedingWords();
                this.totalSeedingWords = seedingWords.Length;

                // Load word definitions
                var wordDefinitions = await mediator.LoadWordDefinitionsJson();
                this.scrapedWordsCount = wordDefinitions.Count;

                // Calculate remaining words
                var processedSet = new HashSet<string>(
                    wordDefinitions.Select(wd => wd.Word.ToLowerInvariant()),
                    StringComparer.OrdinalIgnoreCase);
                
                this.remainingWords = seedingWords.Count(sw => !processedSet.Contains(sw));
                this.progressPercent = this.totalSeedingWords > 0 
                    ? (double)this.scrapedWordsCount / this.totalSeedingWords * 100 
                    : 0;

                // Check JSON file
                var jsonPath = Path.Combine(AppContext.BaseDirectory, this.appSettings.ExportJsonFileName);
                if (File.Exists(jsonPath))
                {
                    var jsonFileInfo = new FileInfo(jsonPath);
                    this.jsonFileSize = jsonFileInfo.Length;
                    this.lastScrapedDate = jsonFileInfo.LastWriteTime;
                }
                else
                {
                    this.jsonFileSize = 0;
                    this.lastScrapedDate = null;
                }

                // Check dictionary ZIP file
                var zipPath = Path.Combine(AppContext.BaseDirectory, this.appSettings.ExportKoboDictionaryFileName);
                if (File.Exists(zipPath))
                {
                    var zipFileInfo = new FileInfo(zipPath);
                    this.dictionaryExists = true;
                    this.dictionaryFileSize = zipFileInfo.Length;
                    this.lastBuildDate = zipFileInfo.LastWriteTime;
                }
                else
                {
                    this.dictionaryExists = false;
                    this.dictionaryFileSize = 0;
                    this.lastBuildDate = null;
                }

                App?.Invoke(() =>
                {
                    UpdatePipelineStatus();
                    UpdateSeedingWordsStats();
                    UpdateScrapedDataStats();
                    UpdateDictionaryBuildStats();
                    UpdateFileStatus();
                    this.statusLabel.Text = $"Dashboard updated at {DateTime.Now:HH:mm:ss}";
                    
                    // Show success notification
                    NotificationHelper.ShowSuccess(
                        "Dashboard Refreshed",
                        $"Successfully loaded dashboard data.\n\n" +
                        $"• Seeding words: {this.totalSeedingWords:N0}\n" +
                        $"• Scraped definitions: {this.scrapedWordsCount:N0}\n" +
                        $"• Dictionary built: {(this.dictionaryExists ? "Yes" : "No")}",
                        App);
                });
            }
        }
        catch (Exception ex)
        {
            App?.Invoke(() =>
            {
                this.statusLabel.Text = $"Error loading dashboard: {ex.Message}";
                NotificationHelper.ShowError(
                    "Dashboard Load Failed",
                    $"Failed to load dashboard data:\n\n{ex.Message}",
                    App);
            });
        }
        finally
        {
            this.isLoading = false;
        }
    }

    private void UpdatePipelineStatus()
    {
        var sb = new StringBuilder();
        
        // Step 1: Extract (check if we have seeding words)
        var extractStatus = this.totalSeedingWords > 0 ? "✓" : "○";
        var extractText = this.totalSeedingWords > 0 ? "Ready" : "Pending";
        
        // Step 2: Scrape (check progress)
        string scrapeStatus;
        string scrapeText;
        if (this.scrapedWordsCount == 0)
        {
            scrapeStatus = "○";
            scrapeText = "Pending";
        }
        else if (this.remainingWords > 0)
        {
            scrapeStatus = "⚠";
            scrapeText = $"{this.scrapedWordsCount:N0}/{this.totalSeedingWords:N0} ({this.progressPercent:F1}%)";
        }
        else
        {
            scrapeStatus = "✓";
            scrapeText = "Complete";
        }
        
        // Step 3: Process (same as scrape for now)
        var processStatus = this.scrapedWordsCount > 0 ? "✓" : "○";
        var processText = this.scrapedWordsCount > 0 ? "Ready" : "Pending";
        
        // Step 4: Build (check if dictionary exists)
        var buildStatus = this.dictionaryExists ? "✓" : "○";
        var buildText = this.dictionaryExists ? "Complete" : "Pending";

        sb.AppendLine($"[1. Extract] {extractStatus} {extractText}  →  [2. Scrape] {scrapeStatus} {scrapeText}  →  [3. Process] {processStatus} {processText}  →  [4. Build] {buildStatus} {buildText}");
        
        this.pipelineStatusLabel.Text = sb.ToString();
    }

    private void UpdateSeedingWordsStats()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Total words: {this.totalSeedingWords:N0}");
        sb.AppendLine($"Scraped: {this.scrapedWordsCount:N0}");
        sb.AppendLine($"Remaining: {this.remainingWords:N0}");
        sb.AppendLine($"Progress: {this.progressPercent:F1}%");
        sb.AppendLine();
        sb.AppendLine($"File: {this.appSettings.SeedingWordsFileName}");
        
        this.seedingWordsStatsLabel.Text = sb.ToString();
    }

    private void UpdateScrapedDataStats()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Definitions: {this.scrapedWordsCount:N0}");
        
        if (this.jsonFileSize > 0)
        {
            var sizeMB = this.jsonFileSize / 1024.0 / 1024.0;
            var sizeKB = this.jsonFileSize / 1024.0;
            var sizeText = sizeMB >= 1.0 ? $"{sizeMB:F2} MB" : $"{sizeKB:F2} KB";
            sb.AppendLine($"File size: {sizeText}");
        }
        else
        {
            sb.AppendLine("File size: N/A");
        }

        if (this.lastScrapedDate.HasValue)
        {
            var elapsed = DateTime.Now - this.lastScrapedDate.Value;
            string timeAgo;
            if (elapsed.TotalMinutes < 1)
            {
                timeAgo = "just now";
            }
            else if (elapsed.TotalHours < 1)
            {
                timeAgo = $"{(int)elapsed.TotalMinutes}m ago";
            }
            else if (elapsed.TotalDays < 1)
            {
                timeAgo = $"{(int)elapsed.TotalHours}h ago";
            }
            else
            {
                timeAgo = $"{(int)elapsed.TotalDays}d ago";
            }
            sb.AppendLine($"Updated: {timeAgo}");
        }
        else
        {
            sb.AppendLine("Updated: Never");
        }
        
        sb.AppendLine();
        sb.AppendLine($"File: {this.appSettings.ExportJsonFileName}");
        
        this.scrapedDataStatsLabel.Text = sb.ToString();
    }

    private void UpdateDictionaryBuildStats()
    {
        var sb = new StringBuilder();
        
        if (this.dictionaryExists)
        {
            sb.AppendLine("Status: ✓ Built");
            
            var sizeMB = this.dictionaryFileSize / 1024.0 / 1024.0;
            var sizeKB = this.dictionaryFileSize / 1024.0;
            var sizeText = sizeMB >= 1.0 ? $"{sizeMB:F2} MB" : $"{sizeKB:F2} KB";
            sb.AppendLine($"Size: {sizeText}");
            
            if (this.lastBuildDate.HasValue)
            {
                sb.AppendLine($"Built: {this.lastBuildDate.Value:yyyy-MM-dd}");
                sb.AppendLine($"Time: {this.lastBuildDate.Value:HH:mm:ss}");
            }
        }
        else
        {
            sb.AppendLine("Status: ○ Not built");
            sb.AppendLine("Size: N/A");
            sb.AppendLine("Last build: Never");
        }
        
        sb.AppendLine();
        sb.AppendLine($"File: {this.appSettings.ExportKoboDictionaryFileName}");
        
        this.dictionaryBuildStatsLabel.Text = sb.ToString();
    }

    private void UpdateFileStatus()
    {
        var sb = new StringBuilder();
        
        // Check seeding words file
        var seedingPath = Path.Combine(AppContext.BaseDirectory, this.appSettings.SeedingWordsFileName);
        if (File.Exists(seedingPath))
        {
            var fileInfo = new FileInfo(seedingPath);
            var sizeKB = fileInfo.Length / 1024.0;
            sb.AppendLine($"✓ {this.appSettings.SeedingWordsFileName} ({sizeKB:F2} KB)");
        }
        else
        {
            sb.AppendLine($"✗ {this.appSettings.SeedingWordsFileName} (not found)");
        }
        
        // Check JSON file
        var jsonPath = Path.Combine(AppContext.BaseDirectory, this.appSettings.ExportJsonFileName);
        if (File.Exists(jsonPath))
        {
            var fileInfo = new FileInfo(jsonPath);
            var sizeMB = fileInfo.Length / 1024.0 / 1024.0;
            var sizeText = sizeMB >= 1.0 ? $"{sizeMB:F2} MB" : $"{fileInfo.Length / 1024.0:F2} KB";
            sb.AppendLine($"✓ {this.appSettings.ExportJsonFileName} ({sizeText})");
        }
        else
        {
            sb.AppendLine($"✗ {this.appSettings.ExportJsonFileName} (not found)");
        }
        
        // Check dictionary ZIP file
        var zipPath = Path.Combine(AppContext.BaseDirectory, this.appSettings.ExportKoboDictionaryFileName);
        if (File.Exists(zipPath))
        {
            var fileInfo = new FileInfo(zipPath);
            var sizeMB = fileInfo.Length / 1024.0 / 1024.0;
            var sizeText = sizeMB >= 1.0 ? $"{sizeMB:F2} MB" : $"{fileInfo.Length / 1024.0:F2} KB";
            sb.AppendLine($"✓ {this.appSettings.ExportKoboDictionaryFileName} ({sizeText})");
        }
        else
        {
            sb.AppendLine($"✗ {this.appSettings.ExportKoboDictionaryFileName} (not found)");
        }
        
        this.fileStatusLabel.Text = sb.ToString();
    }
}
