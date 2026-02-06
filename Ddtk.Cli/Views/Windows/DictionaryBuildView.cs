using System.Collections.Specialized;
using Ddtk.Cli.Components;
using Ddtk.Cli.Services;
using Ddtk.Cli.ViewModels;
using Ddtk.Cli.Views;
using ReactiveUI;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Views.Windows;

public class DictionaryBuildView : BaseView<DictionaryBuildViewModel>
{
    // UI Components
    private Label loadedWordsLabel = null!;
    private Button loadButton = null!;
    private Button startProcessingButton = null!;
    private ProgressBar processingProgressBar = null!;
    private Label processingPercentLabel = null!;
    private Label processingStatusLabel = null!;
    private Button startBuildButton = null!;
    private ProgressBar buildProgressBar = null!;
    private Label buildPercentLabel = null!;
    private Label buildStatusLabel = null!;
    private TextView activityLogView = null!;
    private Button viewOutputButton = null!;
    private Button buildAllButton = null!;
    private Label statusLabel = null!;

    public DictionaryBuildView(MainMenuBar menu, MainStatusBar statusBar, DictionaryBuildViewModel viewModel) : base(viewModel)
    {
        Title = string.Empty;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.None;
        Arrangement = ViewArrangement.Resizable;

        FrameView window = new()
        {
            Title = "Dictionary Building",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        InitializeComponents(window);
        
        Add(menu, window, statusBar);
    }

    public override void InitializeLayout()
    {
        // Already initialized in constructor
    }

    private void InitializeComponents(FrameView window)
    {
        // Load section (Step 1)
        FrameView loadFrame = new()
        {
            Title = "Step 1: Load Word Definitions",
            X = 2,
            Y = 1,
            Width = Dim.Fill() - 4,
            Height = 5
        };

        Label loadInfoLabel = new()
        {
            Text = "Load word definitions from JSON file created during web scraping",
            X = 1,
            Y = 0
        };

        loadedWordsLabel = new Label
        {
            Text = "Loaded: 0 word definitions",
            X = 1,
            Y = 1
        };

        loadButton = new Button
        {
            Text = "Load from JSON",
            X = 1,
            Y = 2
        };

        loadFrame.Add(loadInfoLabel, loadedWordsLabel, loadButton);

        // Processing section (Step 2)
        FrameView processingFrame = new()
        {
            Title = "Step 2: Process Word Definitions",
            X = 2,
            Y = Pos.Bottom(loadFrame) + 1,
            Width = Dim.Fill() - 4,
            Height = 6
        };

        Label processingInfoLabel = new()
        {
            Text = "Merge, clean, and sort word definitions for Kobo format",
            X = 1,
            Y = 0
        };

        startProcessingButton = new Button
        {
            Text = "Start Processing",
            X = 1,
            Y = 1,
            Enabled = false
        };

        Label processingProgressLabel = new()
        {
            Text = "Progress:",
            X = 1,
            Y = 3
        };

        processingProgressBar = new ProgressBar
        {
            X = Pos.Right(processingProgressLabel) + 1,
            Y = 3,
            Width = Dim.Fill() - 2 - processingProgressLabel.Text.Length - 10,
            Height = 1
        };

        processingPercentLabel = new Label
        {
            Text = "0%",
            X = Pos.Right(processingProgressBar) + 1,
            Y = 3,
            Width = 8
        };

        processingStatusLabel = new Label
        {
            Text = "Not started",
            X = 1,
            Y = 4,
            Width = Dim.Fill() - 2
        };

        processingFrame.Add(
            processingInfoLabel,
            startProcessingButton,
            processingProgressLabel, processingProgressBar, processingPercentLabel,
            processingStatusLabel
        );

        // Build section (Step 3)
        FrameView buildFrame = new()
        {
            Title = "Step 3: Build Kobo Dictionary ZIP",
            X = 2,
            Y = Pos.Bottom(processingFrame) + 1,
            Width = Dim.Fill() - 4,
            Height = 6
        };

        Label buildInfoLabel = new()
        {
            Text = "Create final Kobo dictionary ZIP file with HTML files and index",
            X = 1,
            Y = 0
        };

        startBuildButton = new Button
        {
            Text = "Start Build",
            X = 1,
            Y = 1,
            Enabled = false
        };

        Label buildProgressLabel = new()
        {
            Text = "Progress:",
            X = 1,
            Y = 3
        };

        buildProgressBar = new ProgressBar
        {
            X = Pos.Right(buildProgressLabel) + 1,
            Y = 3,
            Width = Dim.Fill() - 2 - buildProgressLabel.Text.Length - 10,
            Height = 1
        };

        buildPercentLabel = new Label
        {
            Text = "0%",
            X = Pos.Right(buildProgressBar) + 1,
            Y = 3,
            Width = 8
        };

        buildStatusLabel = new Label
        {
            Text = "Not started",
            X = 1,
            Y = 4,
            Width = Dim.Fill() - 2
        };

        buildFrame.Add(
            buildInfoLabel,
            startBuildButton,
            buildProgressLabel, buildProgressBar, buildPercentLabel,
            buildStatusLabel
        );

        // Activity log
        FrameView logFrame = new()
        {
            Title = "Activity Log",
            X = 2,
            Y = Pos.Bottom(buildFrame) + 1,
            Width = Dim.Fill() - 4,
            Height = 8
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
        viewOutputButton = new Button
        {
            Text = "View Output File",
            X = 2,
            Y = Pos.Bottom(logFrame) + 1,
            Enabled = false
        };

        buildAllButton = new Button
        {
            Text = "Build All (Load + Process + Build)",
            X = Pos.Right(viewOutputButton) + 2,
            Y = Pos.Bottom(logFrame) + 1
        };

        // Status label
        statusLabel = new Label
        {
            Text = "Ready. Click 'Load from JSON' to begin",
            X = 2,
            Y = Pos.AnchorEnd() - 1,
            Width = Dim.Fill() - 4
        };

        window.Add(
            loadFrame,
            processingFrame,
            buildFrame,
            logFrame,
            viewOutputButton, buildAllButton,
            statusLabel
        );
    }

    public override void BindViewModel()
    {
        // Dialog subscription
        ViewModel.DialogRequested += (sender, args) =>
        {
            App?.Invoke(() => DialogService.ShowDialog(App, args.Title, args.Message));
        };

        // Step 1: Load bindings
        ViewModel.WhenAnyValue(vm => vm.LoadedWordsText)
            .Subscribe(text =>
            {
                App?.Invoke(() => loadedWordsLabel.Text = text);
            });

        ViewModel.WhenAnyValue(vm => vm.CanStartProcessing)
            .Subscribe(canStart =>
            {
                App?.Invoke(() => startProcessingButton.Enabled = canStart);
            });

        loadButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.LoadCommand.Execute().Subscribe();
        };

        // Step 2: Processing bindings
        ViewModel.WhenAnyValue(vm => vm.ProcessingFraction)
            .Subscribe(fraction =>
            {
                App?.Invoke(() => processingProgressBar.Fraction = fraction);
            });

        ViewModel.WhenAnyValue(vm => vm.ProcessingPercent)
            .Subscribe(percent =>
            {
                App?.Invoke(() => processingPercentLabel.Text = percent);
            });

        ViewModel.WhenAnyValue(vm => vm.ProcessingStatus)
            .Subscribe(status =>
            {
                App?.Invoke(() => processingStatusLabel.Text = status);
            });

        startProcessingButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.StartProcessingCommand.Execute().Subscribe();
        };

        // Step 3: Build bindings
        ViewModel.WhenAnyValue(vm => vm.CanStartBuild)
            .Subscribe(canStart =>
            {
                App?.Invoke(() => startBuildButton.Enabled = canStart);
            });

        ViewModel.WhenAnyValue(vm => vm.BuildFraction)
            .Subscribe(fraction =>
            {
                App?.Invoke(() => buildProgressBar.Fraction = fraction);
            });

        ViewModel.WhenAnyValue(vm => vm.BuildPercent)
            .Subscribe(percent =>
            {
                App?.Invoke(() => buildPercentLabel.Text = percent);
            });

        ViewModel.WhenAnyValue(vm => vm.BuildStatus)
            .Subscribe(status =>
            {
                App?.Invoke(() => buildStatusLabel.Text = status);
            });

        startBuildButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.StartBuildCommand.Execute().Subscribe();
        };

        // View output binding
        ViewModel.WhenAnyValue(vm => vm.CanViewOutput)
            .Subscribe(canView =>
            {
                App?.Invoke(() => viewOutputButton.Enabled = canView);
            });

        viewOutputButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.ViewOutputCommand.Execute().Subscribe();
        };

        // Build All binding
        buildAllButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.BuildAllCommand.Execute().Subscribe();
        };

        // Status message binding
        ViewModel.WhenAnyValue(vm => vm.StatusMessage)
            .Subscribe(status =>
            {
                App?.Invoke(() => statusLabel.Text = status ?? string.Empty);
            });

        // Activity log binding
        ViewModel.ActivityLog.CollectionChanged += (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset ||
                e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Remove)
            {
                App?.Invoke(() =>
                {
                    var logText = string.Join("\n", ViewModel.ActivityLog);
                    activityLogView.Text = logText;
                    // Auto-scroll to bottom
                    activityLogView.MoveEnd();
                });
            }
        };
    }
}
