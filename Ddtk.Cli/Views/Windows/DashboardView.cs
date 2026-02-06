using System.Reactive.Linq;
using Ddtk.Cli.Components;
using Ddtk.Cli.Services;
using Ddtk.Cli.ViewModels;
using ReactiveUI;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Views.Windows;

public class DashboardView : BaseView<DashboardViewModel>
{
    private readonly MainMenuBar mainMenu;
    private readonly MainStatusBar statusBar;
    
    // UI Controls
    private Label pipelineStatusLabel = new();
    private Label seedingWordsStatsLabel = new();
    private Label scrapedDataStatsLabel = new();
    private Label dictionaryBuildStatsLabel = new();
    
    private Button refreshButton = new();
    private Button extractWordsButton = new();
    private Button viewSeedingButton = new();
    private Button startScrapingButton = new();
    private Button buildDictionaryButton = new();
    
    public DashboardView(
        DashboardViewModel viewModel,
        MainMenuBar mainMenu,
        MainStatusBar statusBar) : base(viewModel)
    {
        this.mainMenu = mainMenu;
        this.statusBar = statusBar;
        
        // Initialize immediately in constructor
        InitializeLayout();
        BindViewModel();
        
        // Start async data loading
        Task.Run(() => viewModel.LoadDashboardDataAsync(false));
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
            Title = "Danish Dictionary to Kobo - Dashboard",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        // Section 1: Quick Actions
        FrameView quickActionsFrame = new()
        {
            Title = "Quick Actions",
            X = 2,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Auto() 
        };
        
        refreshButton = new Button
        {
            Text = "Refresh Data",
            X = 1,
            Y = 1
        };
        
        extractWordsButton = new Button
        {
            Text = "Extract Words",
            X = Pos.Right(refreshButton) + 2,
            Y = 1
        };
        
        viewSeedingButton = new Button
        {
            Text = "View Seeded Words",
            X = Pos.Right(extractWordsButton) + 2,
            Y = 1
        };
        
        startScrapingButton = new Button
        {
            Text = "Start Scraping",
            X = Pos.Right(viewSeedingButton) + 2,
            Y = 1
        };
        
        buildDictionaryButton = new Button
        {
            Text = "Build Dictionary",
            X = Pos.Right(startScrapingButton) + 2,
            Y = 1
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
        
        // Bind ViewModel properties to UI labels
        ViewModel.WhenAnyValue(vm => vm.PipelineStatus)
            .Subscribe(status => 
            {
                App?.Invoke(() => pipelineStatusLabel.Text = status);
            });
        
        ViewModel.WhenAnyValue(vm => vm.SeedingWordsStats)
            .Subscribe(stats => 
            {
                App?.Invoke(() => seedingWordsStatsLabel.Text = stats);
            });
        
        ViewModel.WhenAnyValue(vm => vm.ScrapedDataStats)
            .Subscribe(stats => 
            {
                App?.Invoke(() => scrapedDataStatsLabel.Text = stats);
            });
        
        ViewModel.WhenAnyValue(vm => vm.DictionaryBuildStats)
            .Subscribe(stats => 
            {
                App?.Invoke(() => dictionaryBuildStatsLabel.Text = stats);
            });
        
        ViewModel.WhenAnyValue(vm => vm.StatusMessage)
            .Subscribe(message => 
            {
                if (!string.IsNullOrEmpty(message))
                {
                    App?.Invoke(() => statusBar.SetStatus(message));
                }
            });
        
        // Wire up button commands
        refreshButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.RefreshCommand.Execute().Subscribe();
        };
        
        extractWordsButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.ExtractWordsCommand.Execute().Subscribe();
        };
        
        viewSeedingButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.ViewSeedingCommand.Execute().Subscribe();
        };
        
        startScrapingButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.StartScrapingCommand.Execute().Subscribe();
        };
        
        buildDictionaryButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.BuildDictionaryCommand.Execute().Subscribe();
        };
    }
}
