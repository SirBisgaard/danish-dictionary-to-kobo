using Ddtk.Business.Services;
using Ddtk.Cli.Components;
using Ddtk.Cli.Components.Windows;
using Ddtk.Cli.Helpers;
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

        var mainMenuBar = new MainMenuBar(ChangeWindow);
        var mainStatusBar = new MainStatusBar();
        var dataStatusService = new DataStatusService(new FileSystemRepository(appSettings));
        var windowDataHelper = new WindowDataHelper();
        
        
        BaseWindow w;
        switch (change)
        {
            case WindowChange.ConfigWindow:
                w = new ConfigWindow(mainMenuBar, mainStatusBar);
                break;
            case WindowChange.PreviewWordDefinitionWindow:
                w = new PreviewWordDefinitionWindow(mainMenuBar, mainStatusBar, appSettings);
                break;
            case WindowChange.EpubWordExtractionWindow:
                w = new EpubWordExtractionWindow(mainMenuBar, mainStatusBar, appSettings);
                break;
            case WindowChange.SeededWordsWindow:
                w = new SeededWordsWindow(mainMenuBar, mainStatusBar, appSettings);
                break;
            case WindowChange.WebScrapingWindow:
                w = new WebScrapingWindow(mainMenuBar, mainStatusBar, appSettings);
                break;
            case WindowChange.DictionaryBuildWindow:
                w = new DictionaryBuildWindow(mainMenuBar, mainStatusBar, appSettings);
                break;
            case WindowChange.DashboardWindow:
            default:
                w = new DashboardWindow(mainMenuBar, mainStatusBar,dataStatusService, windowDataHelper, ChangeWindow);
                w.InitializeLayout();
                w.LoadData();
                break;
        }

        app.RequestStop();
        app.Run(w);
    }
    
    public void Dispose()
    {
        app?.Dispose();
    }
}