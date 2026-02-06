using ReactiveUI;

namespace Ddtk.Cli.ViewModels;

public abstract class ViewModelBase : ReactiveObject
{
    // Common properties for all ViewModels
    public bool IsBusy
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? StatusMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    // Dialog support
    public event EventHandler<DialogEventArgs>? DialogRequested;
    
    protected void ShowDialog(string title, string message)
    {
        DialogRequested?.Invoke(this, new DialogEventArgs(title, message));
    }
}
