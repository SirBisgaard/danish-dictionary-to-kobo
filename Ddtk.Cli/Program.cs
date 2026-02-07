using Ddtk.Business;
using Ddtk.Business.Helpers;
using Ddtk.Business.Interfaces;
using Ddtk.Business.Services;
using Ddtk.Cli;
using Ddtk.Cli.Helpers;
using Ddtk.Cli.Interfaces;
using Ddtk.Cli.Services;
using Ddtk.Cli.ViewModels;
using Ddtk.Cli.Views;
using Ddtk.Cli.Views.Components;
using Ddtk.DataAccess;
using Ddtk.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Terminal.Gui.App;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var appSettings = config.Get<AppSettings>();
if (appSettings == null)
{
    throw new InvalidOperationException("AppSettings could not be loaded from configuration.");
}

// Initialize Terminal.Gui with Amber Phosphor theme
Terminal.Gui.Configuration.ConfigurationManager.RuntimeConfig = """{ "Theme": "Amber Phosphor" }""";
Terminal.Gui.Configuration.ConfigurationManager.Enable(Terminal.Gui.Configuration.ConfigLocations.All);

// Create and initialize the Terminal.Gui application
using var app = Application.Create().Init();

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        // Configuration
        services.AddSingleton(appSettings);

        // Terminal.Gui Application 
        services.AddSingleton(app);
        
        // Singleton Services
        services.AddSingleton<FileSystemRepository>();
        services.AddSingleton<DataStatusService>();
        services.AddSingleton<INavigationService, NavigationService>();
        
        // Transient Services
        services.AddTransient<ISeedingWordService, SeedingWordService>();
        services.AddTransient<WindowDataHelper>();
        
        services.AddTransient<MainMenuBar>();
        services.AddTransient<MainStatusBar>();
        
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<WebScrapingViewModel>();
        services.AddTransient<DictionaryBuildViewModel>();
        services.AddTransient<EpubWordExtractionViewModel>();
        services.AddTransient<SeededWordsViewModel>();
        services.AddTransient<PreviewWordDefinitionViewModel>();
        services.AddTransient<ConfigViewModel>();
        
        services.AddTransient<DashboardView>();
        services.AddTransient<WebScrapingView>();
        services.AddTransient<DictionaryBuildView>();
        services.AddTransient<EpubWordExtractionView>();
        services.AddTransient<SeededWordsView>();
        services.AddTransient<PreviewWordDefinitionView>();
        services.AddTransient<ConfigView>();
        
        // TODO: Legacy services should be refactored SOL.
        services.AddSingleton<LoggingService>();
        services.AddTransient<ProcessMediator>();
        services.AddTransient<FileSystemService>();
        services.AddTransient<EpubWordExtractorService>();
        
        // UI Components 
        
        // ViewModels 
        
        // Logging
        services.AddLogging(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddConsole();
        });
    })
    .Build();

// Starting the application by navigating the dashboard. 🚀
var navigationService = host.Services.GetRequiredService<INavigationService>();
navigationService.NavigateTo(WindowChange.DashboardWindow);

