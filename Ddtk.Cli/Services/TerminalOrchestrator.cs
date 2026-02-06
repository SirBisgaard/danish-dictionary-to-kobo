using Ddtk.Business.Services;
using Ddtk.Cli.Components;
using Ddtk.Cli.Helpers;
using Ddtk.Cli.ViewModels;
using Ddtk.Cli.Views;
using Ddtk.Cli.Views.Windows;
using Ddtk.DataAccess;
using Ddtk.Domain;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Services;

public class TerminalOrchestrator(AppSettings appSettings) : IDisposable
{
    private IApplication? app;

    public void InitApp()
    {
        Terminal.Gui.Configuration.ConfigurationManager.RuntimeConfig = """{ "Theme": "Amber Phosphor" }""";
        Terminal.Gui.Configuration.ConfigurationManager.Enable(Terminal.Gui.Configuration.ConfigLocations.All);

        app = Application.Create().Init();
        ChangeWindow(WindowChange.DashboardWindow);
    }

    private void ChangeWindow(WindowChange change)
    {
        if (app is null)
            return;

        // Create navigation service first
        var navigationService = new NavigationService(ChangeWindow);
        var mainMenuBar = new MainMenuBar(navigationService);
        var mainStatusBar = new MainStatusBar();
        var dataStatusService = new DataStatusService(new FileSystemRepository(appSettings));
        var windowDataHelper = new WindowDataHelper();
        
        Window window; // Changed from BaseWindow to Window (base class)
        switch (change)
        {
            // NEW MVVM VIEWS
            case WindowChange.DashboardWindow:
                var dashboardVm = new DashboardViewModel(dataStatusService, windowDataHelper, navigationService);
                var dashboardView = new DashboardView(dashboardVm, mainMenuBar, mainStatusBar);
                dashboardView.InitializeLayout();
                dashboardView.BindViewModel();
                Task.Run(() => dashboardVm.LoadDashboardDataAsync(false));
                window = dashboardView;
                break;
            
            case WindowChange.WebScrapingWindow:
                var scrapingVm = new WebScrapingViewModel(appSettings);
                var scrapingView = new WebScrapingView(scrapingVm, mainMenuBar, mainStatusBar);
                scrapingView.InitializeLayout();
                scrapingView.BindViewModel();
                window = scrapingView;
                break;
            
            case WindowChange.ConfigWindow:
                var configVm = new ConfigViewModel();
                var configView = new ConfigView(configVm, mainMenuBar, mainStatusBar);
                configView.InitializeLayout();
                configView.BindViewModel();
                window = configView;
                break;
            
            case WindowChange.PreviewWordDefinitionWindow:
                var previewVm = new PreviewWordDefinitionViewModel(appSettings);
                var previewView = new PreviewWordDefinitionView(previewVm, mainMenuBar, mainStatusBar);
                previewView.InitializeLayout();
                previewView.BindViewModel();
                window = previewView;
                break;
            
            case WindowChange.SeededWordsWindow:
                var seededWordsVm = new SeededWordsViewModel(appSettings);
                var seededWordsView = new SeededWordsView(seededWordsVm, mainMenuBar, mainStatusBar);
                seededWordsView.InitializeLayout();
                seededWordsView.BindViewModel();
                window = seededWordsView;
                break;
            
            case WindowChange.EpubWordExtractionWindow:
                var epubVm = new EpubWordExtractionViewModel(appSettings);
                var epubView = new EpubWordExtractionView(epubVm, mainMenuBar, mainStatusBar);
                epubView.InitializeLayout();
                epubView.BindViewModel();
                window = epubView;
                break;
            
            case WindowChange.DictionaryBuildWindow:
                var dictionaryBuildVm = new DictionaryBuildViewModel(appSettings);
                var dictionaryBuildView = new DictionaryBuildView(mainMenuBar, mainStatusBar, dictionaryBuildVm);
                dictionaryBuildView.InitializeLayout();
                dictionaryBuildView.BindViewModel();
                window = dictionaryBuildView;
                break;
            default:
                // Default to dashboard
                var defaultVM = new DashboardViewModel(dataStatusService, windowDataHelper, navigationService);
                var defaultView = new DashboardView(defaultVM, mainMenuBar, mainStatusBar);
                defaultView.InitializeLayout();
                defaultView.BindViewModel();
                window = defaultView;
                break;
        }

        app.RequestStop();
        app.Run(window);
    }
    
    public void Dispose()
    {
        app?.Dispose();
    }
}