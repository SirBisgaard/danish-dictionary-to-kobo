using Ddtk.Cli.Views;
using Terminal.Gui.App;

namespace Ddtk.Cli;

public class TerminalOrchestrator : IDisposable
{
    private IApplication? app;
    
    public void InitApp()
    {
        Terminal.Gui.Configuration.ConfigurationManager.RuntimeConfig = """{ "Theme": "Amber Phosphor" }""";
        Terminal.Gui.Configuration.ConfigurationManager.Enable(Terminal.Gui.Configuration.ConfigLocations.All);

        app = Application.Create().Init();
        using var window = new MainWindow();
        app.Run(window);
    }
    
    public void Dispose()
    {
        app?.Dispose();
    }
}