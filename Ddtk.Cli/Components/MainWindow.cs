using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Components;

public class MainWindow : Window
{
    public MainWindow(MainMenuBar menu, MainStatusBar statusBar)
    {
        Title = string.Empty;
        Width = Dim.Fill();
        Height = Dim.Fill();
        BorderStyle = LineStyle.None;
        Arrangement = ViewArrangement.Resizable;

        FrameView window = new()
        {
            Title = "",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1 // Menu and StatusBar height,
        };
        
        Label label = new()
        {
            Text = "Welcome to Danish Dictionary to Kobo!",
            X = Pos.Center(),
            Y = Pos.Center()
        };
        Label label2 = new()
        {
            Text = "Pres `Alt + F` to open the menu.",
            X = Pos.Center(),
            Y = Pos.Center()+ 1
        };
        
        window.Add(label, label2);
        Add(menu, window, statusBar);
    }
}