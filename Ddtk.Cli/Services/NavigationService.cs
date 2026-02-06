using Ddtk.Cli.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Services;

/// <summary>
/// Implementation of INavigationService that manages window navigation and resolution.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider serviceProvider;
    private IApplication? app;
    
    public NavigationService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }
    
    /// <summary>
    /// Initialize the navigation service with the Terminal.Gui application instance.
    /// </summary>
    public void Initialize(IApplication app)
    {
        this.app = app;
    }
    
    /// <summary>
    /// Navigate to the specified window by resolving it from DI and running it.
    /// </summary>
    public void NavigateTo(WindowChange window)
    {
        if (app is null)
        {
            throw new InvalidOperationException(
                "NavigationService has not been initialized. Call Initialize() with IApplication first.");
        }

        // Resolve window from DI based on enum
        Window resolvedWindow = window switch
        {
            WindowChange.DashboardWindow => serviceProvider.GetRequiredService<DashboardView>(),
            WindowChange.WebScrapingWindow => serviceProvider.GetRequiredService<WebScrapingView>(),
            WindowChange.ConfigWindow => serviceProvider.GetRequiredService<ConfigView>(),
            WindowChange.PreviewWordDefinitionWindow => serviceProvider.GetRequiredService<PreviewWordDefinitionView>(),
            WindowChange.SeededWordsWindow => serviceProvider.GetRequiredService<SeededWordsView>(),
            WindowChange.EpubWordExtractionWindow => serviceProvider.GetRequiredService<EpubWordExtractionView>(),
            WindowChange.DictionaryBuildWindow => serviceProvider.GetRequiredService<DictionaryBuildView>(),
            _ => serviceProvider.GetRequiredService<DashboardView>()
        };

        app.RequestStop();
        app.Run(resolvedWindow);
    }
}
