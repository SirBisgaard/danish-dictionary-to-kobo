using Terminal.Gui.Views;

namespace Ddtk.Cli.Components.Windows;

public abstract class BaseWindow : Window
{
    public abstract void InitializeLayout();
    public abstract void LoadData();
}