using Terminal.Gui.App;

namespace Ddtk.Cli.Services;

/// <summary>
/// Service for navigating between windows in the application.
/// ViewModels can use this to trigger navigation without coupling to the UI layer.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Initialize the navigation service with the Terminal.Gui application instance.
    /// Must be called before any navigation occurs.
    /// </summary>
    /// <param name="app">The Terminal.Gui application instance</param>
    void Initialize(IApplication app);
    
    /// <summary>
    /// Navigate to the specified window.
    /// </summary>
    /// <param name="window">The window to navigate to</param>
    void NavigateTo(WindowChange window);
}
