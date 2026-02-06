using Ddtk.Cli.ViewModels;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Views;

public abstract class BaseView<TViewModel> : Window where TViewModel : ViewModelBase
{
    protected TViewModel ViewModel { get; }
    
    protected BaseView(TViewModel viewModel)
    {
        ViewModel = viewModel;
    }
    
    /// <summary>
    /// Initialize the UI layout and controls (Terminal.Gui components).
    /// This should contain all UI structure setup but no data binding.
    /// </summary>
    public abstract void InitializeLayout();
    
    /// <summary>
    /// Bind ViewModel properties and commands to UI controls.
    /// Subscribe to ViewModel property changes and wire up button commands here.
    /// </summary>
    public abstract void BindViewModel();
}
