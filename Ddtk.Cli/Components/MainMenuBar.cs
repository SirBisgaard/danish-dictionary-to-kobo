using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class MainMenuBar : MenuBar
{
    public MainMenuBar(Action<WindowChange> changeWindow)
    {
        // Create global menu bar at the top (Y = 0)
        var menuItemFile = new MenuBarItem("_File", [
            new MenuItem("_Dashboard", "- Main dashboard", () => changeWindow(WindowChange.MainWindow)),
            new MenuItem("Preview _Definition", "- Preview Kobo HTML formatting", () => changeWindow(WindowChange.PreviewWordDefinitionWindow)),
            new MenuItem("_EPUB Extractor", "- Extract words from EPUB files", () => changeWindow(WindowChange.EpubWordExtractionWindow)),
            new MenuItem("_Seeded Words", "- Manage seeded words list", () => changeWindow(WindowChange.SeededWordsWindow)),
            new MenuItem("_Web Scraping", "- Scrape word definitions from web", () => changeWindow(WindowChange.WebScrapingWindow)),
            new MenuItem("_Build Dictionary", "- Build Kobo dictionary ZIP", () => changeWindow(WindowChange.DictionaryBuildWindow)),
            null!, // Separator
            new MenuItem("C_onfig", "- Configuration", () => changeWindow(WindowChange.ConfigWindow)),
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