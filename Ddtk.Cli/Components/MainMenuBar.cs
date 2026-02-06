using Ddtk.Cli.Services;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class MainMenuBar : MenuBar
{
    public MainMenuBar(Action<WindowChange> changeWindow)
    {
        var menuItemFile = new MenuBarItem("_File", [
            new MenuItem("_Dashboard", "- General Overview", () => changeWindow(WindowChange.DashboardWindow)),
            new MenuItem("_EPUB Extractor", "- Extract words from EPUB files", () => changeWindow(WindowChange.EpubWordExtractionWindow)),
            new MenuItem("_Web Scraping", "- Scrape word definitions from web", () => changeWindow(WindowChange.WebScrapingWindow)),
            new MenuItem("Dictionary _Builder", "- Build Kobo dictionary ZIP", () => changeWindow(WindowChange.DictionaryBuildWindow)),
            null!, // Separator
            new MenuItem("C_onfig", "- Configuration", () => changeWindow(WindowChange.ConfigWindow)),
            new MenuItem("_Seeded Words", "- Manage seeded words list", () => changeWindow(WindowChange.SeededWordsWindow)),
            new MenuItem("Preview _Definition", "- Preview Kobo HTML formatting", () => changeWindow(WindowChange.PreviewWordDefinitionWindow)),
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