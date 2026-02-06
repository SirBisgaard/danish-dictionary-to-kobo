using System.Reactive.Linq;
using Ddtk.Cli.Components;
using Ddtk.Cli.Services;
using Ddtk.Cli.ViewModels;
using ReactiveUI;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Views.Windows;

public class WebScrapingView : BaseView<WebScrapingViewModel>
{
    private readonly MainMenuBar mainMenu;
    private readonly MainStatusBar statusBar;
    
    // UI Controls
    private TextField workerCountField = new();
    private Button startButton = new();
    private Button stopButton = new();
    private ProgressBar progressBar = new();
    private Label progressPercentLabel = new();
    private Label wordsScrapedLabel = new();
    private Label queueSizeLabel = new();
    private Label elapsedTimeLabel = new();
    private TextView activityLogView = new();
    private Button saveResultsButton = new();
    private Label statusLabel = new();
    
    public WebScrapingView(
        WebScrapingViewModel viewModel,
        MainMenuBar mainMenu,
        MainStatusBar statusBar) : base(viewModel)
    {
        this.mainMenu = mainMenu;
        this.statusBar = statusBar;
    }
    
    public override void InitializeLayout()
    {
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
            Text = ViewModel.WorkerCount.ToString(),
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

        stopButton = new Button
        {
            Text = "Stop",
            X = Pos.Right(startButton) + 2,
            Y = Pos.Bottom(configFrame) + 1,
            Enabled = false
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

        Button viewResultsButton = new()
        {
            Text = "View Results",
            X = Pos.Right(saveResultsButton) + 2,
            Y = Pos.Bottom(logFrame) + 1,
            Enabled = false
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
        
        Add(mainMenu, window, statusBar);
    }
    
    public override void BindViewModel()
    {
        // Subscribe to dialog requests from ViewModel
        ViewModel.DialogRequested += (sender, args) =>
        {
            App?.Invoke(() =>
            {
                DialogService.ShowDialog(App, args.Title, args.Message);
            });
        };
        
        // Bind progress bar
        ViewModel.WhenAnyValue(vm => vm.ProgressFraction)
            .Subscribe(fraction => 
            {
                App?.Invoke(() => progressBar.Fraction = fraction);
            });
        
        ViewModel.WhenAnyValue(vm => vm.ProgressPercent)
            .Subscribe(percent => 
            {
                App?.Invoke(() => progressPercentLabel.Text = percent);
            });
        
        // Bind status labels
        ViewModel.WhenAnyValue(vm => vm.WordsScraped)
            .Subscribe(scraped => 
            {
                App?.Invoke(() => wordsScrapedLabel.Text = scraped);
            });
        
        ViewModel.WhenAnyValue(vm => vm.QueueSize)
            .Subscribe(size => 
            {
                App?.Invoke(() => queueSizeLabel.Text = size);
            });
        
        ViewModel.WhenAnyValue(vm => vm.ElapsedTime)
            .Subscribe(time => 
            {
                App?.Invoke(() => elapsedTimeLabel.Text = time);
            });
        
        // Bind button states
        ViewModel.WhenAnyValue(vm => vm.CanStart)
            .Subscribe(enabled => 
            {
                App?.Invoke(() => startButton.Enabled = enabled);
            });
        
        ViewModel.WhenAnyValue(vm => vm.CanStop)
            .Subscribe(enabled => 
            {
                App?.Invoke(() => stopButton.Enabled = enabled);
            });
        
        ViewModel.WhenAnyValue(vm => vm.SaveResultsEnabled)
            .Subscribe(enabled => 
            {
                App?.Invoke(() => saveResultsButton.Enabled = enabled);
            });
        
        // Bind status message
        ViewModel.WhenAnyValue(vm => vm.StatusMessage)
            .Subscribe(message => 
            {
                if (!string.IsNullOrEmpty(message))
                {
                    App?.Invoke(() =>
                    {
                        statusLabel.Text = message;
                        statusBar.SetStatus(message);
                    });
                }
            });
        
        // Bind activity log (collection changes)
        ViewModel.ActivityLog.CollectionChanged += (s, e) =>
        {
            App?.Invoke(() => 
            {
                var logText = string.Join("\n", ViewModel.ActivityLog);
                activityLogView.Text = logText;
                activityLogView.MoveEnd(); // Auto-scroll to bottom
            });
        };
        
        // Bind worker count field to ViewModel
        workerCountField.TextChanged += (s, e) =>
        {
            if (int.TryParse(workerCountField.Text, out int value))
            {
                ViewModel.WorkerCount = value;
            }
        };
        
        // Wire up button commands
        startButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.StartScrapingCommand.Execute().Subscribe();
        };
        
        stopButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.StopScrapingCommand.Execute().Subscribe();
        };
        
        saveResultsButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.SaveResultsCommand.Execute().Subscribe();
        };
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ViewModel.Dispose();
        }
        base.Dispose(disposing);
    }
}
