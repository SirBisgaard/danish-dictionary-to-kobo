using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class SeededWordsWindow : Window
{
    public SeededWordsWindow(MainMenuBar menu, MainStatusBar statusBar)
    {
        Title = string.Empty;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.None;
        Arrangement = ViewArrangement.Resizable;

        FrameView window = new()
        {
            Title = "Seeded Words Management",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1 // Menu and StatusBar height
        };
        
        Label label = new()
        {
            Text = "Seeded Words Management Window - Coming in Phase 2",
            X = Pos.Center(),
            Y = Pos.Center()
        };
        
        window.Add(label);
        Add(menu, window, statusBar);
    }
}
