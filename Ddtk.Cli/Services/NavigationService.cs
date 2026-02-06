using Ddtk.Cli.Interfaces;
using Ddtk.Cli.Views;
using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Services;

/// <summary>
/// Implementation of INavigationService that manages window navigation and resolution.
/// </summary>
public class NavigationService(IServiceProvider serviceProvider, IApplication app) : INavigationService
{
    /// <summary>
    /// Navigate to the specified window by resolving it from DI and running it.
    /// </summary>
    public void NavigateTo(WindowChange window)
    {
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

public enum WindowChange
{
    DashboardWindow,
    ConfigWindow,
    PreviewWordDefinitionWindow,
    EpubWordExtractionWindow,
    SeededWordsWindow,
    WebScrapingWindow,
    DictionaryBuildWindow
}