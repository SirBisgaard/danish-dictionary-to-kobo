using Ddtk.Cli.Components;
using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace Ddtk.Cli;

public class TerminalOrchestrator : IDisposable
{
    private IApplication? app;
    
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