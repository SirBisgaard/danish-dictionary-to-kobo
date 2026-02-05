using Ddtk.Cli.Components;
using Ddtk.Domain;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace Ddtk.Cli;

public class TerminalOrchestrator : IDisposable
{
    private readonly AppSettings appSettings;
    private IApplication? app;

    public TerminalOrchestrator(AppSettings appSettings)
    {
        this.appSettings = appSettings;
    }
    
    public void InitApp()
    {
        Terminal.Gui.Configuration.ConfigurationManager.RuntimeConfig = """{ "Theme": "Amber Phosphor" }""";
        Terminal.Gui.Configuration.ConfigurationManager.Enable(Terminal.Gui.Configuration.ConfigLocations.All);

        app = Application.Create().Init();
        ChangeWindow(WindowChange.MainWindow);
    }

    private void ChangeWindow(WindowChange change)
    {
        if (app is null)
            return;

        var mainMenuBar = new MainMenuBar(ChangeWindow);
        var mainStatusBar = new MainStatusBar();
        
        Window w;
        switch (change)
        {
            case WindowChange.StatusWindow:
                w = new StatusWindow(mainMenuBar, mainStatusBar);
                break;
            case WindowChange.ConfigWindow:
                w = new ConfigWindow(mainMenuBar, mainStatusBar);
                break;
            case WindowChange.PreviewWordDefinitionWindow:
                w = new PreviewWordDefinitionWindow(mainMenuBar, mainStatusBar, this.appSettings);
                break;
            case WindowChange.EpubWordExtractionWindow:
                w = new EpubWordExtractionWindow(mainMenuBar, mainStatusBar);
                break;
            case WindowChange.SeededWordsWindow:
                w = new SeededWordsWindow(mainMenuBar, mainStatusBar, this.appSettings);
                break;
            case WindowChange.WebScrapingWindow:
                w = new WebScrapingWindow(mainMenuBar, mainStatusBar);
                break;
            case WindowChange.DictionaryBuildWindow:
                w = new DictionaryBuildWindow(mainMenuBar, mainStatusBar);
                break;
            case WindowChange.MainWindow:
            default:
                w = new MainWindow(mainMenuBar, mainStatusBar);
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