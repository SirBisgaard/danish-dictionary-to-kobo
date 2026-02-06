namespace Ddtk.Cli.ViewModels;

public class DialogEventArgs : EventArgs
{
    public string Title { get; }
    public string Message { get; }
    
    public DialogEventArgs(string title, string message)
    {
        Title = title;
        Message = message;
    }
}
