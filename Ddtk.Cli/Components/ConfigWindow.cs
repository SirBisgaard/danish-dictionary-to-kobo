using System.Text.Json;
using Ddtk.Domain;
using Microsoft.Extensions.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class ConfigWindow : Window
{
    private readonly TextField cultureField;
    private readonly TextField logFileNameField;
    private readonly TextField seedingWordsFileNameField;
    private readonly TextField exportJsonFileNameField;
    private readonly TextField exportKoboDictionaryFileNameField;
    private readonly TextField exportKoboDictionaryTestHtmlFileNameField;
    private readonly TextField dictionaryCopyRightTextField;
    private readonly TextField webScraperBaseAddressField;
    private readonly TextField webScraperWordAddressField;
    private readonly TextField webScraperStartUrlField;
    private readonly TextField webScraperWorkerCountField;
    private readonly Label statusLabel;
    private readonly string appSettingsPath;

    public ConfigWindow(MainMenuBar menu, MainStatusBar statusBar)
    {
        Title = string.Empty;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.None;
        Arrangement = ViewArrangement.Resizable;

        this.appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        FrameView window = new()
        {
            Title = "Configuration",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1 // Menu and StatusBar height
        };

        int currentY = 1;
        int labelWidth = 35;
        int fieldWidth = 50;

        // Culture
        window.Add(CreateLabel("Culture:", 2, currentY, labelWidth));
        this.cultureField = CreateTextField(labelWidth + 2, currentY, fieldWidth);
        window.Add(this.cultureField);
        currentY += 2;

        // LogFileName
        window.Add(CreateLabel("Log File Name:", 2, currentY, labelWidth));
        this.logFileNameField = CreateTextField(labelWidth + 2, currentY, fieldWidth);
        window.Add(this.logFileNameField);
        currentY += 2;

        // SeedingWordsFileName
        window.Add(CreateLabel("Seeding Words File Name:", 2, currentY, labelWidth));
        this.seedingWordsFileNameField = CreateTextField(labelWidth + 2, currentY, fieldWidth);
        window.Add(this.seedingWordsFileNameField);
        currentY += 2;

        // ExportJsonFileName
        window.Add(CreateLabel("Export JSON File Name:", 2, currentY, labelWidth));
        this.exportJsonFileNameField = CreateTextField(labelWidth + 2, currentY, fieldWidth);
        window.Add(this.exportJsonFileNameField);
        currentY += 2;

        // ExportKoboDictionaryFileName
        window.Add(CreateLabel("Export Kobo Dictionary File:", 2, currentY, labelWidth));
        this.exportKoboDictionaryFileNameField = CreateTextField(labelWidth + 2, currentY, fieldWidth);
        window.Add(this.exportKoboDictionaryFileNameField);
        currentY += 2;

        // ExportKoboDictionaryTestHtmlFileName
        window.Add(CreateLabel("Export Kobo Dictionary Test HTML:", 2, currentY, labelWidth));
        this.exportKoboDictionaryTestHtmlFileNameField = CreateTextField(labelWidth + 2, currentY, fieldWidth);
        window.Add(this.exportKoboDictionaryTestHtmlFileNameField);
        currentY += 2;

        // DictionaryCopyRightText
        window.Add(CreateLabel("Dictionary Copyright Text:", 2, currentY, labelWidth));
        this.dictionaryCopyRightTextField = CreateTextField(labelWidth + 2, currentY, fieldWidth);
        window.Add(this.dictionaryCopyRightTextField);
        currentY += 2;

        // WebScraperBaseAddress
        window.Add(CreateLabel("Web Scraper Base Address:", 2, currentY, labelWidth));
        this.webScraperBaseAddressField = CreateTextField(labelWidth + 2, currentY, fieldWidth);
        window.Add(this.webScraperBaseAddressField);
        currentY += 2;

        // WebScraperWordAddress
        window.Add(CreateLabel("Web Scraper Word Address:", 2, currentY, labelWidth));
        this.webScraperWordAddressField = CreateTextField(labelWidth + 2, currentY, fieldWidth);
        window.Add(this.webScraperWordAddressField);
        currentY += 2;

        // WebScraperStartUrl
        window.Add(CreateLabel("Web Scraper Start URL:", 2, currentY, labelWidth));
        this.webScraperStartUrlField = CreateTextField(labelWidth + 2, currentY, fieldWidth);
        window.Add(this.webScraperStartUrlField);
        currentY += 2;

        // WebScraperWorkerCount
        window.Add(CreateLabel("Web Scraper Worker Count:", 2, currentY, labelWidth));
        this.webScraperWorkerCountField = CreateTextField(labelWidth + 2, currentY, fieldWidth);
        window.Add(this.webScraperWorkerCountField);

        // Status label for feedback
        this.statusLabel = new Label
        {
            Text = string.Empty,
            X = 2,
            Y = Pos.AnchorEnd() - 4,
            Width = Dim.Fill() - 4
        };
        window.Add(this.statusLabel);

       

        // Save button (bottom right, before Cancel)
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
            Save();
        };
        
        // Cancel button (bottom right)
        Button cancelButton = new()
        {
            Text = "Cancel",
            X = Pos.AnchorEnd() - 13,
            Y = Pos.AnchorEnd() - 1
        };
        cancelButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            Cancel();
        };
        window.Add(cancelButton, saveButton);

        Add(menu, window, statusBar);

        // Load current settings
        LoadSettings();
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

    private static TextField CreateTextField(int x, int y, int width)
    {
        return new TextField
        {
            X = x,
            Y = y,
            Width = width
        };
    }

    private void LoadSettings()
    {
        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var appSettings = config.Get<AppSettings>();
            if (appSettings == null)
            {
                SetStatusMessage("ERROR: Could not load appsettings.json", true);
                return;
            }

            // Populate fields
            this.cultureField.Text = appSettings.Culture.Name;
            this.logFileNameField.Text = appSettings.LogFileName;
            this.seedingWordsFileNameField.Text = appSettings.SeedingWordsFileName;
            this.exportJsonFileNameField.Text = appSettings.ExportJsonFileName;
            this.exportKoboDictionaryFileNameField.Text = appSettings.ExportKoboDictionaryFileName;
            this.exportKoboDictionaryTestHtmlFileNameField.Text = appSettings.ExportKoboDictionaryTestHtmlFileName;
            this.dictionaryCopyRightTextField.Text = appSettings.DictionaryCopyRightText;
            this.webScraperBaseAddressField.Text = appSettings.WebScraperBaseAddress.ToString();
            this.webScraperWordAddressField.Text = appSettings.WebScraperWordAddress.ToString();
            this.webScraperStartUrlField.Text = appSettings.WebScraperStartUrl.ToString();
            this.webScraperWorkerCountField.Text = appSettings.WebScraperWorkerCount.ToString();

            SetStatusMessage("Settings loaded successfully", false);
        }
        catch (Exception ex)
        {
            SetStatusMessage($"ERROR: Failed to load settings: {ex.Message}", true);
        }
    }

    private void Save()
    {
        try
        {
            // Validate worker count
            if (!int.TryParse(this.webScraperWorkerCountField.Text, out int workerCount) || workerCount <= 0)
            {
                SetStatusMessage("ERROR: Web Scraper Worker Count must be a positive integer.", true);
                return;
            }

            // Validate URLs
            if (!Uri.TryCreate(this.webScraperBaseAddressField.Text, UriKind.Absolute, out _))
            {
                SetStatusMessage("ERROR: Web Scraper Base Address must be a valid URL.", true);
                return;
            }

            if (!Uri.TryCreate(this.webScraperWordAddressField.Text, UriKind.Absolute, out _))
            {
                SetStatusMessage("ERROR: Web Scraper Word Address must be a valid URL.", true);
                return;
            }

            if (!Uri.TryCreate(this.webScraperStartUrlField.Text, UriKind.Absolute, out _))
            {
                SetStatusMessage("ERROR: Web Scraper Start URL must be a valid URL.", true);
                return;
            }

            // Create JSON object
            var settings = new Dictionary<string, object>
            {
                ["Culture"] = this.cultureField.Text ?? string.Empty,
                ["LogFileName"] = this.logFileNameField.Text ?? string.Empty,
                ["SeedingWordsFileName"] = this.seedingWordsFileNameField.Text ?? string.Empty,
                ["ExportJsonFileName"] = this.exportJsonFileNameField.Text ?? string.Empty,
                ["ExportKoboDictionaryFileName"] = this.exportKoboDictionaryFileNameField.Text ?? string.Empty,
                ["ExportKoboDictionaryTestHtmlFileName"] = this.exportKoboDictionaryTestHtmlFileNameField.Text ?? string.Empty,
                ["DictionaryCopyRightText"] = this.dictionaryCopyRightTextField.Text ?? string.Empty,
                ["WebScraperBaseAddress"] = this.webScraperBaseAddressField.Text ?? string.Empty,
                ["WebScraperWordAddress"] = this.webScraperWordAddressField.Text ?? string.Empty,
                ["WebScraperStartUrl"] = this.webScraperStartUrlField.Text ?? string.Empty,
                ["WebScraperWorkerCount"] = workerCount
            };

            // Serialize with indentation
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(settings, options);

            // Write to file
            File.WriteAllText(this.appSettingsPath, json);

            SetStatusMessage("SUCCESS: Settings saved. Please restart the application for changes to take effect.", false);
        }
        catch (Exception ex)
        {
            SetStatusMessage($"ERROR: Failed to save settings: {ex.Message}", true);
        }
    }

    private void Cancel()
    {
        // Just reload the settings to discard changes
        LoadSettings();
        SetStatusMessage("Changes discarded, settings reloaded", false);
    }

    private void SetStatusMessage(string message, bool isError)
    {
        this.statusLabel.Text = message;
        // Terminal.Gui v2 handles coloring differently, keeping simple for now
    }
}