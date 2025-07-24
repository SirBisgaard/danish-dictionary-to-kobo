using Ddtk.Cli;
using Microsoft.Extensions.Configuration;

try
{
    var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .Build();

    var appSettings = config.Get<AppSettings>();
    if (appSettings == null)
    {
        throw new InvalidOperationException("AppSettings could not be loaded from configuration.");
    }


    await using var processMediator = new ProcessMediator(appSettings);

    var skipWebScraping = false;
    if (args.Length > 0)
    {
        skipWebScraping = args.Contains("--skip-web-scraping");
    }

    await processMediator.Run(skipWebScraping);
}
catch (Exception e)
{
    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine("-- Unexpected error occurred --");
    Console.WriteLine(e);
}