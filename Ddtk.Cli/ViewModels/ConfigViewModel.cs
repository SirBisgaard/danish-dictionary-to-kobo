using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using Ddtk.Domain;
using Microsoft.Extensions.Configuration;
using ReactiveUI;

namespace Ddtk.Cli.ViewModels;

public class ConfigViewModel : ViewModelBase
{
    private readonly string appSettingsPath;
    
    // Configuration properties
    private string culture = string.Empty;
    public string Culture
    {
        get => culture;
        set => this.RaiseAndSetIfChanged(ref culture, value);
    }
    
    private string logFileName = string.Empty;
    public string LogFileName
    {
        get => logFileName;
        set => this.RaiseAndSetIfChanged(ref logFileName, value);
    }
    
    private string seedingWordsFileName = string.Empty;
    public string SeedingWordsFileName
    {
        get => seedingWordsFileName;
        set => this.RaiseAndSetIfChanged(ref seedingWordsFileName, value);
    }
    
    private string wordDefinitionFileName = string.Empty;
    public string WordDefinitionFileName
    {
        get => wordDefinitionFileName;
        set => this.RaiseAndSetIfChanged(ref wordDefinitionFileName, value);
    }
    
    private string koboDictionaryFileName = string.Empty;
    public string KoboDictionaryFileName
    {
        get => koboDictionaryFileName;
        set => this.RaiseAndSetIfChanged(ref koboDictionaryFileName, value);
    }
    
    private string koboDictionaryTestHtmlFileName = string.Empty;
    public string KoboDictionaryTestHtmlFileName
    {
        get => koboDictionaryTestHtmlFileName;
        set => this.RaiseAndSetIfChanged(ref koboDictionaryTestHtmlFileName, value);
    }
    
    private string dictionaryCopyRightText = string.Empty;
    public string DictionaryCopyRightText
    {
        get => dictionaryCopyRightText;
        set => this.RaiseAndSetIfChanged(ref dictionaryCopyRightText, value);
    }
    
    private string webScraperBaseAddress = string.Empty;
    public string WebScraperBaseAddress
    {
        get => webScraperBaseAddress;
        set => this.RaiseAndSetIfChanged(ref webScraperBaseAddress, value);
    }
    
    private string webScraperWordAddress = string.Empty;
    public string WebScraperWordAddress
    {
        get => webScraperWordAddress;
        set => this.RaiseAndSetIfChanged(ref webScraperWordAddress, value);
    }
    
    private string webScraperStartUrl = string.Empty;
    public string WebScraperStartUrl
    {
        get => webScraperStartUrl;
        set => this.RaiseAndSetIfChanged(ref webScraperStartUrl, value);
    }
    
    private string webScraperWorkerCount = string.Empty;
    public string WebScraperWorkerCount
    {
        get => webScraperWorkerCount;
        set => this.RaiseAndSetIfChanged(ref webScraperWorkerCount, value);
    }
    
    // Commands
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadSettingsCommand { get; }
    
    public ConfigViewModel()
    {
        appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        
        // Commands
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
        CancelCommand = ReactiveCommand.Create(Cancel);
        LoadSettingsCommand = ReactiveCommand.CreateFromTask(LoadSettingsAsync);
        
        // Auto-load settings on construction
        LoadSettingsCommand.Execute().Subscribe();
    }
    
    public async Task LoadSettingsAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading settings...";
            
            await Task.Run(() =>
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .Build();
                
                var appSettings = config.Get<AppSettings>();
                if (appSettings == null)
                {
                    StatusMessage = "ERROR: Could not load appsettings.json";
                    return;
                }
                
                // Populate properties
                Culture = appSettings.Culture.Name;
                LogFileName = appSettings.LogFileName;
                SeedingWordsFileName = appSettings.SeedingWordsFileName;
                WordDefinitionFileName = appSettings.WordDefinitionFileName;
                KoboDictionaryFileName = appSettings.KoboDictionaryFileName;
                KoboDictionaryTestHtmlFileName = appSettings.ExportKoboDictionaryTestHtmlFileName;
                DictionaryCopyRightText = appSettings.DictionaryCopyRightText;
                WebScraperBaseAddress = appSettings.WebScraperBaseAddress.ToString();
                WebScraperWordAddress = appSettings.WebScraperWordAddress.ToString();
                WebScraperStartUrl = appSettings.WebScraperStartUrl.ToString();
                WebScraperWorkerCount = appSettings.WebScraperWorkerCount.ToString();
                
                StatusMessage = "Settings loaded successfully";
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"ERROR: Failed to load settings: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Saving settings...";
            
            // Validate worker count
            if (!int.TryParse(WebScraperWorkerCount, out int workerCount) || workerCount <= 0)
            {
                StatusMessage = "ERROR: Web Scraper Worker Count must be a positive integer.";
                ShowDialog("Validation Error", "Web Scraper Worker Count must be a positive integer.");
                IsBusy = false;
                return;
            }
            
            // Validate URLs
            if (!Uri.TryCreate(WebScraperBaseAddress, UriKind.Absolute, out _))
            {
                StatusMessage = "ERROR: Web Scraper Base Address must be a valid URL.";
                ShowDialog("Validation Error", "Web Scraper Base Address must be a valid URL.");
                IsBusy = false;
                return;
            }
            
            if (!Uri.TryCreate(WebScraperWordAddress, UriKind.Absolute, out _))
            {
                StatusMessage = "ERROR: Web Scraper Word Address must be a valid URL.";
                ShowDialog("Validation Error", "Web Scraper Word Address must be a valid URL.");
                IsBusy = false;
                return;
            }
            
            if (!Uri.TryCreate(WebScraperStartUrl, UriKind.Absolute, out _))
            {
                StatusMessage = "ERROR: Web Scraper Start URL must be a valid URL.";
                ShowDialog("Validation Error", "Web Scraper Start URL must be a valid URL.");
                IsBusy = false;
                return;
            }
            
            await Task.Run(() =>
            {
                // Create JSON object
                var settings = new Dictionary<string, object>
                {
                    ["Culture"] = Culture,
                    ["LogFileName"] = LogFileName,
                    ["SeedingWordsFileName"] = SeedingWordsFileName,
                    ["WordDefinitionFileName"] = WordDefinitionFileName,
                    ["KoboDictionaryFileName"] = KoboDictionaryFileName,
                    ["ExportKoboDictionaryTestHtmlFileName"] = KoboDictionaryTestHtmlFileName,
                    ["DictionaryCopyRightText"] = DictionaryCopyRightText,
                    ["WebScraperBaseAddress"] = WebScraperBaseAddress,
                    ["WebScraperWordAddress"] = WebScraperWordAddress,
                    ["WebScraperStartUrl"] = WebScraperStartUrl,
                    ["WebScraperWorkerCount"] = workerCount
                };
                
                // Serialize with indentation
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(settings, options);
                
                // Write to file
                File.WriteAllText(appSettingsPath, json);
            });
            
            StatusMessage = "SUCCESS: Settings saved. Please restart the application for changes to take effect.";
            ShowDialog("Save Successful", "Settings saved successfully.\n\nPlease restart the application for changes to take effect.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"ERROR: Failed to save settings: {ex.Message}";
            ShowDialog("Save Error", $"Failed to save settings:\n\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void Cancel()
    {
        // Reload settings to discard changes
        LoadSettingsCommand.Execute().Subscribe();
        StatusMessage = "Changes discarded, settings reloaded";
    }
}
