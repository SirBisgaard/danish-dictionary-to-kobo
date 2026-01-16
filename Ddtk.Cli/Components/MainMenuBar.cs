using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class MainMenuBar : MenuBar
{
    public MainMenuBar(Action<WindowChange> changeWindow)
    {
        // Create global menu bar at the top (Y = 0)
        var menuItemFile = new MenuBarItem("_File", [
            new MenuItem("_Status", "- Status overview", () => changeWindow(WindowChange.StatusWindow)),
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