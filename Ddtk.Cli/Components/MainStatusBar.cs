using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class MainStatusBar : StatusBar
{
    public MainStatusBar()
    {
        var f1 = new Shortcut(Key.F1, "Help", () => { });
        var esc = new Shortcut(Key.Esc, "Quit", () => App?.RequestStop());
        
        Add(f1, esc);
    }
}