using System.Reactive.Linq;
using Ddtk.Cli.Components;
using Ddtk.Cli.Services;
using Ddtk.Cli.ViewModels;
using ReactiveUI;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Views.Windows;

public class PreviewWordDefinitionView : BaseView<PreviewWordDefinitionViewModel>
{
    private readonly MainMenuBar mainMenu;
    private readonly MainStatusBar statusBar;
    
    // UI Controls
    private readonly TextField wordInputField = new();
    private readonly TextView rawHtmlView = new();
    private readonly TextView humanReadableView = new();
    private readonly Label statusLabel = new();
    
    public PreviewWordDefinitionView(PreviewWordDefinitionViewModel viewModel, MainMenuBar mainMenu, MainStatusBar statusBar) 
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
            Title = "Preview Word Definition",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };
        
        // Input section at top
        Label wordLabel = new()
        {
            Text = "Word:",
            X = 2,
            Y = 1
        };
        
        wordInputField.X = Pos.Right(wordLabel) + 1;
        wordInputField.Y = 1;
        wordInputField.Width = 20;
        
        Button generateButton = new()
        {
            Text = "Generate Preview",
            X = Pos.Right(wordInputField) + 2,
            Y = 1
        };
        generateButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.GeneratePreviewCommand.Execute().Subscribe();
        };
        
        // Split view for raw HTML and human-readable
        FrameView rawHtmlFrame = new()
        {
            Title = "Raw HTML",
            X = 2,
            Y = 3,
            Width = Dim.Percent(50) - 1,
            Height = Dim.Fill() - 5
        };
        
        rawHtmlView.X = 0;
        rawHtmlView.Y = 0;
        rawHtmlView.Width = Dim.Fill();
        rawHtmlView.Height = Dim.Fill();
        rawHtmlView.ReadOnly = true;
        rawHtmlView.WordWrap = false;
        rawHtmlFrame.Add(rawHtmlView);
        
        FrameView humanReadableFrame = new()
        {
            Title = "Human Readable",
            X = Pos.Percent(50) + 1,
            Y = 3,
            Width = Dim.Percent(50) - 3,
            Height = Dim.Fill() - 5
        };
        
        humanReadableView.X = 0;
        humanReadableView.Y = 0;
        humanReadableView.Width = Dim.Fill();
        humanReadableView.Height = Dim.Fill();
        humanReadableView.ReadOnly = true;
        humanReadableView.WordWrap = true;
        humanReadableFrame.Add(humanReadableView);
        
        // Bottom buttons and status
        Button saveButton = new()
        {
            Text = "Save HTML to Test File",
            X = 2,
            Y = Pos.AnchorEnd() - 1
        };
        saveButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.SaveHtmlCommand.Execute().Subscribe();
        };
        
        statusLabel.Text = "Ready";
        statusLabel.X = Pos.Right(saveButton) + 2;
        statusLabel.Y = Pos.AnchorEnd() - 1;
        statusLabel.Width = Dim.Fill() - 2;
        
        window.Add(wordLabel, wordInputField, generateButton, rawHtmlFrame, humanReadableFrame, saveButton, statusLabel);
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
        
        // Two-way binding for word input
        ViewModel.WhenAnyValue(vm => vm.Word)
            .Subscribe(value =>
            {
                App?.Invoke(() => wordInputField.Text = value ?? string.Empty);
            });
        wordInputField.TextChanged += (s, e) =>
        {
            ViewModel.Word = wordInputField.Text ?? string.Empty;
        };
        
        // One-way bindings for read-only views
        ViewModel.WhenAnyValue(vm => vm.RawHtml)
            .Subscribe(value =>
            {
                App?.Invoke(() => rawHtmlView.Text = value ?? string.Empty);
            });
        
        ViewModel.WhenAnyValue(vm => vm.HumanReadable)
            .Subscribe(value =>
            {
                App?.Invoke(() => humanReadableView.Text = value ?? string.Empty);
            });
    }
}
