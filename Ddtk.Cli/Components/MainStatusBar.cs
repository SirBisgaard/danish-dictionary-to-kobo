using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class MainStatusBar : StatusBar
{
    private readonly Label statusLabel = new();

    public MainStatusBar()
    {
        // var f1 = new Shortcut(Key.F1, "Help", () =>
        // {
        //     wordDefinitionCount.Text = "Word Definitions: 23.453";
        //     seededWordCount.Text = "Seed Words: 34";
        //     scrapeFileCount.Text = "Scrape Files: 423";
        // });
        // var esc = new Shortcut(Key.Esc, "Quit", () => App?.RequestStop());

        Add(statusLabel);
    }

    public void SetStatus(string status)
    {
        statusLabel.Text =  $"Status: {status}";
    }
}