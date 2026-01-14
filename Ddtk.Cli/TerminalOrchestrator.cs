using Ddtk.Domain;
using Terminal.Gui.App;

namespace Ddtk.Cli;

public class TerminalOrchestrator(AppSettings appSettings) : IDisposable
{
    private IApplication? App = null;
    
    public void InitApp()
    {
        
        Terminal.Gui.Configuration.ConfigurationManager.RuntimeConfig = """{ "Theme": "Amber Phosphor" }""";
        Terminal.Gui.Configuration.ConfigurationManager.Enable(Terminal.Gui.Configuration.ConfigLocations.All);

        App = Application.Create().Init();
        using var window = new MainWindow();
        App.Run(window);
    }
    
    public void Dispose()
    {
        App?.Dispose();
    }
}