using Terminal.Gui.Views;

namespace Ddtk.Cli;

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Ddtk - Danish Dictionary to Kobo";

        var menuItemFile = new MenuBarItem("_File");

        var menu = new MenuBar([menuItemFile]);
        var statusBar = new StatusBar();

        Add(menu, statusBar);
    }
}