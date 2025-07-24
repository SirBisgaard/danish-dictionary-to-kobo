using System.Text;
using System.Threading.Channels;

namespace Ddtk.Cli.Services;

public class LoggingService : IAsyncDisposable
{
    private readonly AppSettings appSettings;
    private int lastOverwriteLength;

    private readonly Channel<LogEntry> channel = Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions{ SingleReader = true, SingleWriter = false});
    private readonly Task worker;

    private record LogEntry(string Message)
    {
        public string Message { get; set; } = Message;
        public bool Overwrite { get; set; }
    }

    public LoggingService(AppSettings appSettings)
    {
        this.appSettings = appSettings;
        
        worker = Task.Run(WriteLogsToConsole);
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, appSettings.LogFileName)))
        {
            File.Delete(Path.Combine(AppContext.BaseDirectory, appSettings.LogFileName));
        }
    }
    
    
    private async Task WriteLogsToConsole()
    {
        await foreach (var entry in channel.Reader.ReadAllAsync())
        {
            if (entry.Overwrite)
            {
                Console.Write('\r');

                if (lastOverwriteLength > entry.Message.Length)
                {
                    Console.Write(new string(' ', lastOverwriteLength));
                    Console.Write('\r');
                }

                Console.Write(entry.Message);
                lastOverwriteLength = entry.Message.Length;
            }
            else
            {
                if (lastOverwriteLength > 0)
                {
                    Console.WriteLine();
                    lastOverwriteLength = 0;
                }

                Console.WriteLine(entry.Message);
            }
            
            await File.AppendAllLinesAsync(Path.Combine(AppContext.BaseDirectory, appSettings.LogFileName), [$"{DateTime.Now:yyyy-MM-dd hh\\:mm\\:ss} {entry.Message}"], Encoding.UTF8);
        }
    }
    
    public void Log()
    {
        Log(string.Empty);
    }

    public void Log(string message)
    {
        channel.Writer.TryWrite(new LogEntry(message));
    }

    public void LogOverwrite(string message)
    {
        channel.Writer.TryWrite(new LogEntry(message) { Overwrite = true });
    }
    
    public async ValueTask DisposeAsync()
    {
        channel.Writer.Complete();
        await worker;
        
        GC.SuppressFinalize(this);
    }
}