using System.Collections.Specialized;
using System.Reactive.Linq;
using Ddtk.Cli.Components;
using Ddtk.Cli.Services;
using Ddtk.Cli.ViewModels;
using ReactiveUI;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Views.Windows;

public class EpubWordExtractionView : BaseView<EpubWordExtractionViewModel>
{
    private readonly MainMenuBar mainMenu;
    private readonly MainStatusBar statusBar;
    
    // UI Controls
    private readonly ListView filesListView = new();
    private readonly ProgressBar progressBar = new();
    private readonly Label progressLabel = new();
    private readonly Label statsLabel = new();
    private readonly Label statusLabel = new();
    private readonly Button extractButton = new();
    private readonly Button saveButton = new();
    private readonly Button viewWordsButton = new();
    private readonly Button exportButton = new();
    private FrameView? filesFrame;
    
    public EpubWordExtractionView(EpubWordExtractionViewModel viewModel, MainMenuBar mainMenu, MainStatusBar statusBar) 
        : base(viewModel)
    {
        this.mainMenu = mainMenu;
        this.statusBar = statusBar;
        
        // Initialize immediately in constructor
        InitializeLayout();
        BindViewModel();
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
            Title = "EPUB Word Extraction",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };
        
        // File selection buttons
        Button selectFilesButton = new()
        {
            Text = "Select Files",
            X = 2,
            Y = 1
        };
        selectFilesButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.SelectFilesCommand.Execute().Subscribe();
        };
        
        Button selectFolderButton = new()
        {
            Text = "Select Folder",
            X = Pos.Right(selectFilesButton) + 2,
            Y = 1
        };
        selectFolderButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.SelectFolderCommand.Execute().Subscribe();
        };
        
        Button clearButton = new()
        {
            Text = "Clear Selection",
            X = Pos.Right(selectFolderButton) + 2,
            Y = 1
        };
        clearButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.ClearSelectionCommand.Execute().Subscribe();
        };
        
        // Files list
        filesFrame = new FrameView
        {
            Title = "Selected Files (0)",
            X = 2,
            Y = 3,
            Width = Dim.Fill() - 4,
            Height = 8
        };
        
        filesListView.X = 0;
        filesListView.Y = 0;
        filesListView.Width = Dim.Fill();
        filesListView.Height = Dim.Fill();
        filesFrame.Add(filesListView);
        
        // Extract button
        extractButton.Text = "Extract Words";
        extractButton.X = 2;
        extractButton.Y = Pos.Bottom(filesFrame) + 1;
        extractButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.ExtractWordsCommand.Execute().Subscribe();
        };
        
        // Progress section
        Label progressTitleLabel = new()
        {
            Text = "Progress:",
            X = 2,
            Y = Pos.Bottom(filesFrame) + 3
        };
        
        progressBar.X = Pos.Right(progressTitleLabel) + 1;
        progressBar.Y = Pos.Bottom(filesFrame) + 3;
        progressBar.Width = Dim.Fill() - 4 - progressTitleLabel.Text.Length;
        progressBar.Height = 1;
        
        progressLabel.Text = "Ready";
        progressLabel.X = 2;
        progressLabel.Y = Pos.Bottom(filesFrame) + 4;
        progressLabel.Width = Dim.Fill() - 4;
        
        // Statistics section
        statsLabel.Text = "";
        statsLabel.X = 2;
        statsLabel.Y = Pos.Bottom(filesFrame) + 5;
        statsLabel.Width = Dim.Fill() - 4;
        statsLabel.Height = 4;
        
        // Action buttons
        saveButton.Text = "Save to Seeding Words";
        saveButton.X = 2;
        saveButton.Y = Pos.AnchorEnd() - 3;
        saveButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.SaveToSeedingWordsCommand.Execute().Subscribe();
        };
        
        viewWordsButton.Text = "View Words";
        viewWordsButton.X = Pos.Right(saveButton) + 2;
        viewWordsButton.Y = Pos.AnchorEnd() - 3;
        viewWordsButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.ViewWordsCommand.Execute().Subscribe();
        };
        
        exportButton.Text = "Export to File";
        exportButton.X = Pos.Right(viewWordsButton) + 2;
        exportButton.Y = Pos.AnchorEnd() - 3;
        exportButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.ExportCommand.Execute().Subscribe();
        };
        
        // Status label
        statusLabel.Text = "Select EPUB files to extract words";
        statusLabel.X = 2;
        statusLabel.Y = Pos.AnchorEnd() - 1;
        statusLabel.Width = Dim.Fill() - 4;
        
        window.Add(
            selectFilesButton, selectFolderButton, clearButton,
            filesFrame,
            extractButton,
            progressTitleLabel, progressBar, progressLabel,
            statsLabel,
            saveButton, viewWordsButton, exportButton,
            statusLabel
        );
        
        Add(mainMenu, window, statusBar);
    }
    
    public override void BindViewModel()
    {
        // Dialog subscription
        ViewModel.DialogRequested += (sender, args) =>
        {
            App?.Invoke(() => DialogService.ShowDialog(App, args.Title, args.Message));
        };
        
        // File dialogs - handled by View
        ViewModel.SelectFilesRequested += (sender, e) =>
        {
            App?.Invoke(() =>
            {
                var openDialog = new OpenDialog
                {
                    Title = "Select EPUB Files",
                    AllowsMultipleSelection = true
                };
                
                App?.Run(openDialog);
                
                if (!openDialog.Canceled && openDialog.FilePaths.Count > 0)
                {
                    ViewModel.AddFiles(openDialog.FilePaths);
                }
            });
        };
        
        ViewModel.SelectFolderRequested += (sender, e) =>
        {
            App?.Invoke(() =>
            {
                var openDialog = new OpenDialog
                {
                    Title = "Select Folder with EPUB Files",
                    AllowsMultipleSelection = false
                };
                
                App?.Run(openDialog);
                
                if (!openDialog.Canceled && openDialog.FilePaths.Count > 0)
                {
                    ViewModel.AddFilesFromFolder(openDialog.FilePaths[0]);
                }
            });
        };
        
        ViewModel.ViewWordsRequested += (sender, args) =>
        {
            App?.Invoke(() =>
            {
                var dialog = new Dialog
                {
                    Title = args.Title,
                    Width = Dim.Percent(80),
                    Height = Dim.Percent(80)
                };
                
                var textView = new TextView
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill() - 1,
                    ReadOnly = true,
                    Text = args.Message
                };
                
                var okButton = new Button
                {
                    Text = "OK",
                    X = Pos.Center(),
                    Y = Pos.AnchorEnd()
                };
                okButton.Accepting += (s, e) =>
                {
                    e.Handled = true;
                    App?.RequestStop();
                };
                
                dialog.Add(textView, okButton);
                App?.Run(dialog);
            });
        };
        
        // Status message binding
        ViewModel.WhenAnyValue(vm => vm.StatusMessage)
            .Subscribe(value =>
            {
                App?.Invoke(() => statusLabel.Text = value ?? string.Empty);
            });
        
        // Files frame title binding
        ViewModel.WhenAnyValue(vm => vm.FilesFrameTitle)
            .Subscribe(value =>
            {
                App?.Invoke(() =>
                {
                    if (filesFrame != null)
                    {
                        filesFrame.Title = value ?? "Selected Files (0)";
                    }
                });
            });
        
        // Progress bindings
        ViewModel.WhenAnyValue(vm => vm.ProgressFraction)
            .Subscribe(value =>
            {
                App?.Invoke(() => progressBar.Fraction = value);
            });
        
        ViewModel.WhenAnyValue(vm => vm.ProgressText)
            .Subscribe(value =>
            {
                App?.Invoke(() => progressLabel.Text = value ?? "Ready");
            });
        
        // Statistics binding
        ViewModel.WhenAnyValue(vm => vm.Statistics)
            .Subscribe(value =>
            {
                App?.Invoke(() => statsLabel.Text = value ?? string.Empty);
            });
        
        // Button enabled states
        ViewModel.WhenAnyValue(vm => vm.CanExtract)
            .Subscribe(value =>
            {
                App?.Invoke(() => extractButton.Enabled = value);
            });
        
        ViewModel.WhenAnyValue(vm => vm.CanSave)
            .Subscribe(value =>
            {
                App?.Invoke(() => saveButton.Enabled = value);
            });
        
        ViewModel.WhenAnyValue(vm => vm.CanViewWords)
            .Subscribe(value =>
            {
                App?.Invoke(() => viewWordsButton.Enabled = value);
            });
        
        ViewModel.WhenAnyValue(vm => vm.CanExport)
            .Subscribe(value =>
            {
                App?.Invoke(() => exportButton.Enabled = value);
            });
        
        // Selected files collection binding
        ViewModel.SelectedFiles.CollectionChanged += (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset ||
                e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Remove)
            {
                App?.Invoke(() => filesListView.SetSource(ViewModel.SelectedFiles));
            }
        };
        
        // Initial load
        filesListView.SetSource(ViewModel.SelectedFiles);
    }
}
