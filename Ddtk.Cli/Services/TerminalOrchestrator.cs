using Terminal.Gui.App;

namespace Ddtk.Cli.Services;

public class TerminalOrchestrator : IDisposable
{
    private readonly INavigationService navigationService;
    private IApplication? app;

    public TerminalOrchestrator(INavigationService navigationService)
    {
        this.navigationService = navigationService;
    }

    public void InitApp()
    {
        Terminal.Gui.Configuration.ConfigurationManager.RuntimeConfig = """{ "Theme": "Amber Phosphor" }""";
        Terminal.Gui.Configuration.ConfigurationManager.Enable(Terminal.Gui.Configuration.ConfigLocations.All);

        app = Application.Create().Init();
        
        // Initialize navigation service with the app instance
        navigationService.Initialize(app);
        
        // Navigate to the dashboard
        navigationService.NavigateTo(WindowChange.DashboardWindow);
    }
    
    public void Dispose()
    {
        app?.Dispose();
    }
}