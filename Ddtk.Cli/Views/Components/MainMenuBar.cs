using Ddtk.Cli.Interfaces;
using Ddtk.Cli.Services;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Views.Components;

public class MainMenuBar : MenuBar
{
    private readonly INavigationService navigationService;
    
    public MainMenuBar(INavigationService navigationService)
    {
        this.navigationService = navigationService;
        
        var menuItemFile = new MenuBarItem("_File", [
            new MenuItem("_Dashboard", "- General Overview", () => navigationService.NavigateTo(WindowChange.DashboardWindow)),
            new MenuItem("_EPUB Extractor", "- Extract words from EPUB files", () => navigationService.NavigateTo(WindowChange.EpubWordExtractionWindow)),
            new MenuItem("_Web Scraping", "- Scrape word definitions from web", () => navigationService.NavigateTo(WindowChange.WebScrapingWindow)),
            new MenuItem("Dictionary _Builder", "- Build Kobo dictionary ZIP", () => navigationService.NavigateTo(WindowChange.DictionaryBuildWindow)),
            null!, // Separator
            new MenuItem("C_onfig", "- Configuration", () => navigationService.NavigateTo(WindowChange.ConfigWindow)),
            new MenuItem("_Seeded Words", "- Manage seeded words list", () => navigationService.NavigateTo(WindowChange.SeededWordsWindow)),
            new MenuItem("Preview _Definition", "- Preview Kobo HTML formatting", () => navigationService.NavigateTo(WindowChange.PreviewWordDefinitionWindow)),
            null!, // Separator
            new MenuItem("_Quit", "- Exit application", () => App?.RequestStop())
        ]);

        Dialog dialog = new()
        {
            Title = "About",
            Width = 30,
            Height = 10,
        };
        dialog.ButtonAlignment = Alignment.Center;
        dialog.AddButton(new() { Text = "OK" });

        var menuItemHelp = new MenuBarItem("_Help", [
            new MenuItem("_About", "- About this application", () => { App?.Run(dialog); })
        ]);

        Add(menuItemFile, menuItemHelp);
    }
}