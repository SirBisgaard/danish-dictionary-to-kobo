using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class DictionaryBuildWindow : Window
{
    public DictionaryBuildWindow(MainMenuBar menu, MainStatusBar statusBar)
    {
        Title = string.Empty;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.None;
        Arrangement = ViewArrangement.Resizable;

        FrameView window = new()
        {
            Title = "Dictionary Building",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1 // Menu and StatusBar height
        };
        
        Label label = new()
        {
            Text = "Dictionary Building Window - Coming in Phase 5",
            X = Pos.Center(),
            Y = Pos.Center()
        };
        
        window.Add(label);
        Add(menu, window, statusBar);
    }
}
