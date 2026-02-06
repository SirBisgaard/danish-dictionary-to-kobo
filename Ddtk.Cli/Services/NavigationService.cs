namespace Ddtk.Cli.Services;

/// <summary>
/// Implementation of INavigationService that delegates to the TerminalOrchestrator's window change callback.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly Action<WindowChange> changeWindowCallback;
    
    public NavigationService(Action<WindowChange> changeWindowCallback)
    {
        this.changeWindowCallback = changeWindowCallback;
    }
    
    public void NavigateTo(WindowChange window)
    {
        changeWindowCallback(window);
    }
}
