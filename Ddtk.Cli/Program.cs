using Ddtk.Domain;
using Microsoft.Extensions.Configuration;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var appSettings = config.Get<AppSettings>();
if (appSettings == null)
{
    throw new InvalidOperationException("AppSettings could not be loaded from configuration.");
}

Terminal.Gui.Configuration.ConfigurationManager.RuntimeConfig = """{ "Theme": "Amber Phosphor" }""";
Terminal.Gui.Configuration.ConfigurationManager.Enable(Terminal.Gui.Configuration.ConfigLocations.All);

using IApplication app = Application.Create();
app.Init();

using Window window = new();
window.Title = "Ddtk - Danish Dictionary to Kobo";

Label label = new()
{
    Text = "Hello, Terminal.Gui v2!",
    X = Pos.Center(),
    Y = Pos.Center()
};
window.Add(label);

app.Run(window);

// try
// {
//     
//
//     await using var processMediator = new ProcessMediator(appSettings);
//
//     var skipWebScraping = false;
//     if (args.Length > 0)
//     {
//         skipWebScraping = args.Contains("--skip-web-scraping");
//     }
//
//     await processMediator.Run(skipWebScraping);
// }
// catch (Exception e)
// {
//     Console.WriteLine();
//     Console.WriteLine();
//     Console.WriteLine("-- Unexpected error occurred --");
//     Console.WriteLine(e);
// }