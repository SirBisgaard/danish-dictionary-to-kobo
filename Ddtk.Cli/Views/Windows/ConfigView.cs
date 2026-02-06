using System.Reactive.Linq;
using Ddtk.Cli.Components;
using Ddtk.Cli.Services;
using Ddtk.Cli.ViewModels;
using ReactiveUI;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Views.Windows;

public class ConfigView : BaseView<ConfigViewModel>
{
    private readonly MainMenuBar mainMenu;
    private readonly MainStatusBar statusBar;
    
    // UI Controls
    private readonly TextField cultureField = new();
    private readonly TextField logFileNameField = new();
    private readonly TextField seedingWordsFileNameField = new();
    private readonly TextField wordDefinitionFileNameField = new();
    private readonly TextField koboDictionaryFileNameField = new();
    private readonly TextField koboDictionaryTestHtmlFileNameField = new();
    private readonly TextField dictionaryCopyRightTextField = new();
    private readonly TextField webScraperBaseAddressField = new();
    private readonly TextField webScraperWordAddressField = new();
    private readonly TextField webScraperStartUrlField = new();
    private readonly TextField webScraperWorkerCountField = new();
    private readonly Label statusLabel = new();
    
    public ConfigView(ConfigViewModel viewModel, MainMenuBar mainMenu, MainStatusBar statusBar) 
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
            Title = "Configuration",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };
        
        int currentY = 1;
        int labelWidth = 35;
        int fieldWidth = 50;
        
        // Culture
        window.Add(CreateLabel("Culture:", 2, currentY, labelWidth));
        cultureField.X = labelWidth + 2;
        cultureField.Y = currentY;
        cultureField.Width = fieldWidth;
        window.Add(cultureField);
        currentY += 2;
        
        // LogFileName
        window.Add(CreateLabel("Log File Name:", 2, currentY, labelWidth));
        logFileNameField.X = labelWidth + 2;
        logFileNameField.Y = currentY;
        logFileNameField.Width = fieldWidth;
        window.Add(logFileNameField);
        currentY += 2;
        
        // SeedingWordsFileName
        window.Add(CreateLabel("Seeding Words File Name:", 2, currentY, labelWidth));
        seedingWordsFileNameField.X = labelWidth + 2;
        seedingWordsFileNameField.Y = currentY;
        seedingWordsFileNameField.Width = fieldWidth;
        window.Add(seedingWordsFileNameField);
        currentY += 2;
        
        // WordDefinitionFileName
        window.Add(CreateLabel("Word Definition File Name:", 2, currentY, labelWidth));
        wordDefinitionFileNameField.X = labelWidth + 2;
        wordDefinitionFileNameField.Y = currentY;
        wordDefinitionFileNameField.Width = fieldWidth;
        window.Add(wordDefinitionFileNameField);
        currentY += 2;
        
        // KoboDictionaryFileName
        window.Add(CreateLabel("Kobo Dictionary File:", 2, currentY, labelWidth));
        koboDictionaryFileNameField.X = labelWidth + 2;
        koboDictionaryFileNameField.Y = currentY;
        koboDictionaryFileNameField.Width = fieldWidth;
        window.Add(koboDictionaryFileNameField);
        currentY += 2;
        
        // KoboDictionaryTestHtmlFileName
        window.Add(CreateLabel("Kobo Dictionary Test HTML:", 2, currentY, labelWidth));
        koboDictionaryTestHtmlFileNameField.X = labelWidth + 2;
        koboDictionaryTestHtmlFileNameField.Y = currentY;
        koboDictionaryTestHtmlFileNameField.Width = fieldWidth;
        window.Add(koboDictionaryTestHtmlFileNameField);
        currentY += 2;
        
        // DictionaryCopyRightText
        window.Add(CreateLabel("Dictionary Copyright Text:", 2, currentY, labelWidth));
        dictionaryCopyRightTextField.X = labelWidth + 2;
        dictionaryCopyRightTextField.Y = currentY;
        dictionaryCopyRightTextField.Width = fieldWidth;
        window.Add(dictionaryCopyRightTextField);
        currentY += 2;
        
        // WebScraperBaseAddress
        window.Add(CreateLabel("Web Scraper Base Address:", 2, currentY, labelWidth));
        webScraperBaseAddressField.X = labelWidth + 2;
        webScraperBaseAddressField.Y = currentY;
        webScraperBaseAddressField.Width = fieldWidth;
        window.Add(webScraperBaseAddressField);
        currentY += 2;
        
        // WebScraperWordAddress
        window.Add(CreateLabel("Web Scraper Word Address:", 2, currentY, labelWidth));
        webScraperWordAddressField.X = labelWidth + 2;
        webScraperWordAddressField.Y = currentY;
        webScraperWordAddressField.Width = fieldWidth;
        window.Add(webScraperWordAddressField);
        currentY += 2;
        
        // WebScraperStartUrl
        window.Add(CreateLabel("Web Scraper Start URL:", 2, currentY, labelWidth));
        webScraperStartUrlField.X = labelWidth + 2;
        webScraperStartUrlField.Y = currentY;
        webScraperStartUrlField.Width = fieldWidth;
        window.Add(webScraperStartUrlField);
        currentY += 2;
        
        // WebScraperWorkerCount
        window.Add(CreateLabel("Web Scraper Worker Count:", 2, currentY, labelWidth));
        webScraperWorkerCountField.X = labelWidth + 2;
        webScraperWorkerCountField.Y = currentY;
        webScraperWorkerCountField.Width = fieldWidth;
        window.Add(webScraperWorkerCountField);
        
        // Status label for feedback
        statusLabel.Text = string.Empty;
        statusLabel.X = 2;
        statusLabel.Y = Pos.AnchorEnd() - 4;
        statusLabel.Width = Dim.Fill() - 4;
        window.Add(statusLabel);
        
        // Save button
        Button saveButton = new()
        {
            Text = "Save",
            X = Pos.AnchorEnd() - 2,
            Y = Pos.AnchorEnd() - 1,
            IsDefault = true
        };
        saveButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.SaveCommand.Execute().Subscribe();
        };
        
        // Cancel button
        Button cancelButton = new()
        {
            Text = "Cancel",
            X = Pos.AnchorEnd() - 13,
            Y = Pos.AnchorEnd() - 1
        };
        cancelButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            ViewModel.CancelCommand.Execute().Subscribe();
        };
        
        window.Add(cancelButton, saveButton);
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
                App?.Invoke(() => statusLabel.Text = value ?? string.Empty);
            });
        
        // Two-way bindings for text fields
        ViewModel.WhenAnyValue(vm => vm.Culture)
            .Subscribe(value =>
            {
                App?.Invoke(() => cultureField.Text = value ?? string.Empty);
            });
        cultureField.TextChanged += (s, e) =>
        {
            ViewModel.Culture = cultureField.Text ?? string.Empty;
        };
        
        ViewModel.WhenAnyValue(vm => vm.LogFileName)
            .Subscribe(value =>
            {
                App?.Invoke(() => logFileNameField.Text = value ?? string.Empty);
            });
        logFileNameField.TextChanged += (s, e) =>
        {
            ViewModel.LogFileName = logFileNameField.Text ?? string.Empty;
        };
        
        ViewModel.WhenAnyValue(vm => vm.SeedingWordsFileName)
            .Subscribe(value =>
            {
                App?.Invoke(() => seedingWordsFileNameField.Text = value ?? string.Empty);
            });
        seedingWordsFileNameField.TextChanged += (s, e) =>
        {
            ViewModel.SeedingWordsFileName = seedingWordsFileNameField.Text ?? string.Empty;
        };
        
        ViewModel.WhenAnyValue(vm => vm.WordDefinitionFileName)
            .Subscribe(value =>
            {
                App?.Invoke(() => wordDefinitionFileNameField.Text = value ?? string.Empty);
            });
        wordDefinitionFileNameField.TextChanged += (s, e) =>
        {
            ViewModel.WordDefinitionFileName = wordDefinitionFileNameField.Text ?? string.Empty;
        };
        
        ViewModel.WhenAnyValue(vm => vm.KoboDictionaryFileName)
            .Subscribe(value =>
            {
                App?.Invoke(() => koboDictionaryFileNameField.Text = value ?? string.Empty);
            });
        koboDictionaryFileNameField.TextChanged += (s, e) =>
        {
            ViewModel.KoboDictionaryFileName = koboDictionaryFileNameField.Text ?? string.Empty;
        };
        
        ViewModel.WhenAnyValue(vm => vm.KoboDictionaryTestHtmlFileName)
            .Subscribe(value =>
            {
                App?.Invoke(() => koboDictionaryTestHtmlFileNameField.Text = value ?? string.Empty);
            });
        koboDictionaryTestHtmlFileNameField.TextChanged += (s, e) =>
        {
            ViewModel.KoboDictionaryTestHtmlFileName = koboDictionaryTestHtmlFileNameField.Text ?? string.Empty;
        };
        
        ViewModel.WhenAnyValue(vm => vm.DictionaryCopyRightText)
            .Subscribe(value =>
            {
                App?.Invoke(() => dictionaryCopyRightTextField.Text = value ?? string.Empty);
            });
        dictionaryCopyRightTextField.TextChanged += (s, e) =>
        {
            ViewModel.DictionaryCopyRightText = dictionaryCopyRightTextField.Text ?? string.Empty;
        };
        
        ViewModel.WhenAnyValue(vm => vm.WebScraperBaseAddress)
            .Subscribe(value =>
            {
                App?.Invoke(() => webScraperBaseAddressField.Text = value ?? string.Empty);
            });
        webScraperBaseAddressField.TextChanged += (s, e) =>
        {
            ViewModel.WebScraperBaseAddress = webScraperBaseAddressField.Text ?? string.Empty;
        };
        
        ViewModel.WhenAnyValue(vm => vm.WebScraperWordAddress)
            .Subscribe(value =>
            {
                App?.Invoke(() => webScraperWordAddressField.Text = value ?? string.Empty);
            });
        webScraperWordAddressField.TextChanged += (s, e) =>
        {
            ViewModel.WebScraperWordAddress = webScraperWordAddressField.Text ?? string.Empty;
        };
        
        ViewModel.WhenAnyValue(vm => vm.WebScraperStartUrl)
            .Subscribe(value =>
            {
                App?.Invoke(() => webScraperStartUrlField.Text = value ?? string.Empty);
            });
        webScraperStartUrlField.TextChanged += (s, e) =>
        {
            ViewModel.WebScraperStartUrl = webScraperStartUrlField.Text ?? string.Empty;
        };
        
        ViewModel.WhenAnyValue(vm => vm.WebScraperWorkerCount)
            .Subscribe(value =>
            {
                App?.Invoke(() => webScraperWorkerCountField.Text = value ?? string.Empty);
            });
        webScraperWorkerCountField.TextChanged += (s, e) =>
        {
            ViewModel.WebScraperWorkerCount = webScraperWorkerCountField.Text ?? string.Empty;
        };
    }
    
    private static Label CreateLabel(string text, int x, int y, int width)
    {
        return new Label
        {
            Text = text,
            X = x,
            Y = y,
            Width = width
        };
    }
}
