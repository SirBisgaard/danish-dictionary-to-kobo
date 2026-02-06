using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Ddtk.Cli.Services;

/// <summary>
/// Helper class for showing dialogs.
/// </summary>
public static class DialogService
{
    /// <summary>
    /// Show a dia
    /// </summary>
    /// <param name="app">Application instance</param>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Message to display</param>
    public static void ShowDialog(IApplication app, string title, string message)
    {
        var dialog = new Dialog
        {
            Title = title,
            Width = Dim.Percent(60),
            Height = Dim.Percent(40)
        };

        // Message label
        var messageLabel = new Label
        {
            Text = message,
            X = 2,
            Y = 2,
            Width = Dim.Fill() - 4,
            Height = Dim.Fill() - 5,
            TextAlignment = Alignment.Start
        };

        // OK button
        var okButton = new Button
        {
            Text = "OK",
            X = Pos.Center(),
            Y = Pos.AnchorEnd() - 1
        };
        okButton.Accepting += (_, _) => app.RequestStop();

        dialog.Add(messageLabel, okButton);
        app.Run(dialog);
    }
}
