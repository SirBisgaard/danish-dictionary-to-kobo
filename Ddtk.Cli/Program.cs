using Ddtk.Cli;
using Ddtk.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var appSettings = config.Get<AppSettings>();
if (appSettings == null)
{
    throw new InvalidOperationException("AppSettings could not be loaded from configuration.");
}


var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        // Config
        services.AddSingleton(appSettings);
        
        // Services
        services.AddTransient<TerminalOrchestrator>();

        // Windows

        services.AddLogging(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddConsole();
            }
        );
    })
    .Build();

using var tui = ActivatorUtilities.CreateInstance<TerminalOrchestrator>(host.Services);
tui.InitApp();


