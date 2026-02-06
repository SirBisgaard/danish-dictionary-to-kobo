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

public class SeededWordsView : BaseView<SeededWordsViewModel>
{
    private readonly MainMenuBar mainMenu;
    private readonly MainStatusBar statusBar;
    
    // UI Controls
    private readonly TextField searchField = new();
    private readonly ListView wordsListView = new();
    private readonly Label statsLabel = new();
    private readonly Label statusLabel = new();
    private FrameView? listFrame;
    
    public SeededWordsView(SeededWordsViewModel viewModel, MainMenuBar mainMenu, MainStatusBar statusBar) 
        : base(viewModel)
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
        
        searchField.X = Pos.Right(searchLabel) + 1;
        searchField.Y = 1;
        searchField.Width = 30;
        
        Button clearSearchButton = new()
        {
            Text = "Clear",
            X = Pos.Right(searchField) + 2,
            Y = 1
        };
        clearSearchButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.ClearSearchCommand.Execute().Subscribe();
        };
        
        // List view frame
        listFrame = new FrameView
        {
            Title = "Seeded Words (0 total)",
            X = 2,
            Y = 3,
            Width = Dim.Fill() - 4,
            Height = Dim.Fill() - 13
        };
        
        wordsListView.X = 0;
        wordsListView.Y = 0;
        wordsListView.Width = Dim.Fill();
        wordsListView.Height = Dim.Fill();
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
            ViewModel.LoadWordsCommand.Execute().Subscribe();
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
            ViewModel.ExportCommand.Execute().Subscribe();
        };
        
        // Statistics section
        statsLabel.Text = "Statistics: Loading...";
        statsLabel.X = 2;
        statsLabel.Y = Pos.Bottom(listFrame) + 3;
        statsLabel.Width = Dim.Fill() - 4;
        statsLabel.Height = 3;
        
        // Status label
        statusLabel.Text = "Loading words...";
        statusLabel.X = 2;
        statusLabel.Y = Pos.AnchorEnd() - 1;
        statusLabel.Width = Dim.Fill() - 4;
        
        window.Add(
            searchLabel, searchField, clearSearchButton,
            listFrame,
            refreshButton, exportButton,
            statsLabel, statusLabel
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
        
        // Status message binding
        ViewModel.WhenAnyValue(vm => vm.StatusMessage)
            .Subscribe(value =>
            {
                App?.Invoke(() => statusLabel.Text = value ?? "Ready");
            });
        
        // Two-way binding for search text
        ViewModel.WhenAnyValue(vm => vm.SearchText)
            .Subscribe(value =>
            {
                App?.Invoke(() => searchField.Text = value ?? string.Empty);
            });
        searchField.TextChanged += (s, e) =>
        {
            ViewModel.SearchText = searchField.Text ?? string.Empty;
        };
        
        // List frame title binding
        ViewModel.WhenAnyValue(vm => vm.ListFrameTitle)
            .Subscribe(value =>
            {
                App?.Invoke(() =>
                {
                    if (listFrame != null)
                    {
                        listFrame.Title = value ?? "Seeded Words";
                    }
                });
            });
        
        // Statistics binding
        ViewModel.WhenAnyValue(vm => vm.Statistics)
            .Subscribe(value =>
            {
                App?.Invoke(() => statsLabel.Text = value ?? string.Empty);
            });
        
        // Filtered words collection binding
        ViewModel.FilteredWords.CollectionChanged += (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset ||
                e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Remove)
            {
                App?.Invoke(() => wordsListView.SetSource(ViewModel.FilteredWords));
            }
        };
        
        // Initial load
        wordsListView.SetSource(ViewModel.FilteredWords);
    }
}
