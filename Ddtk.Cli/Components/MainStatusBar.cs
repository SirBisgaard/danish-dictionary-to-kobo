using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class MainStatusBar : StatusBar
{
    private readonly Label wordDefinitionCount = new();
    private readonly Label seededWordCount = new();
    private readonly Label scrapeFileCount = new();

    public MainStatusBar()
    {
        var f1 = new Shortcut(Key.F1, "Help", () =>
        {
            wordDefinitionCount.Text = "Word Definitions: 23.453";
            seededWordCount.Text = "Seed Words: 34";
            scrapeFileCount.Text = "Scrape Files: 423";
        });

        wordDefinitionCount.Text = "Word Definitions: 0";
        seededWordCount.Text = "Seed Words: 0";
        scrapeFileCount.Text = "Scrape Files: 0";
        // var esc = new Shortcut(Key.Esc, "Quit", () => App?.RequestStop());

        Add(f1, wordDefinitionCount, seededWordCount, scrapeFileCount);
    }
}