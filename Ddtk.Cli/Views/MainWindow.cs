using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Views;

/// <summary>
/// Main application window containing the primary UI content.
/// Menu and status bar are managed globally at the application level.
/// </summary>
public class MainWindow : Window
{
    public MainWindow()
    {
        Title = string.Empty;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.None;
        Arrangement = ViewArrangement.Resizable;

        // Create global menu bar at the top (Y = 0)
        var menuItemFile = new MenuBarItem("_File", [
            new MenuItem("_Quit", "Exit application", () => App?.RequestStop())
        ]);

        var menuItemHelp = new MenuBarItem("_Help", [
            new MenuItem("_About", "About this application", () => { })
        ]);

        var menu = new MenuBar([menuItemFile, menuItemHelp]);

        // Create global status bar at the bottom
        var statusBar = new StatusBar([
            new Shortcut(Key.F1, "Help", () => { }),
            new Shortcut(Key.Esc, "Quit", () => App?.RequestStop())
        ]);

        FrameView window = new ()
        {
            Title = "",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1 // Menu and StatusBar height,
        };
        Label label = new ()
        {
            Text = "Welcome to Danish Dictionary to Kobo!",
            X = Pos.Center (),
            Y = Pos.Center ()
        };
        window.Add (label);
        Add(menu, window,statusBar);
    }
}