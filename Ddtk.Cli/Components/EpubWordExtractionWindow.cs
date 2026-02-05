using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class EpubWordExtractionWindow : Window
{
    public EpubWordExtractionWindow(MainMenuBar menu, MainStatusBar statusBar)
    {
        Title = string.Empty;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.None;
        Arrangement = ViewArrangement.Resizable;

        FrameView window = new()
        {
            Title = "EPUB Word Extraction",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1 // Menu and StatusBar height
        };
        
        Label label = new()
        {
            Text = "EPUB Word Extraction Window - Coming in Phase 3",
            X = Pos.Center(),
            Y = Pos.Center()
        };
        
        window.Add(label);
        Add(menu, window, statusBar);
    }
}
