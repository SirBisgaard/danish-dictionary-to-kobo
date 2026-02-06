namespace Ddtk.Cli.Services;

/// <summary>
/// Service for navigating between windows in the application.
/// ViewModels can use this to trigger navigation without coupling to the UI layer.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigate to the specified window.
    /// </summary>
    /// <param name="window">The window to navigate to</param>
    void NavigateTo(WindowChange window);
}
