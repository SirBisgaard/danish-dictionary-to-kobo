using System.Threading.Channels;
using Ddtk.Cli.Helpers;
using Ddtk.Domain.Models;

namespace Ddtk.Cli.Services;

public class BackupService : IAsyncDisposable
{
    private readonly FileSystemService fileSystemService;
    private readonly StreamWriter jsonBackupStream;

    private readonly Channel<(WordDefinition wordDefinition, string htmlDocument)> wordDefinitionChannel;
    private readonly Task consumerTask;
    
    public BackupService(StreamWriter jsonBackupStream, FileSystemService fileSystemService)
    {
        this.fileSystemService = fileSystemService;
        this.jsonBackupStream = jsonBackupStream;
        this.jsonBackupStream.AutoFlush = true;
        
        wordDefinitionChannel = Channel.CreateUnbounded<(WordDefinition wordDefinition, string htmlDocument)>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = false,
        });

        consumerTask = Task.Run(ConsumeBackupTasks);
    }

    public ValueTask AddToQueue(WordDefinition wordDefinition, string htmlDocument) => wordDefinitionChannel.Writer.WriteAsync(new ValueTuple<WordDefinition, string>(wordDefinition, htmlDocument));

    private async Task ConsumeBackupTasks()
    {
        var counter = 0L;
        var firstWrite = true;
        await jsonBackupStream.WriteLineAsync("[");
        await foreach (var tuple in wordDefinitionChannel.Reader.ReadAllAsync())
        {
            await fileSystemService.SaveHtmlDocument(tuple.wordDefinition.Word, tuple.htmlDocument);
            
            if (!firstWrite)
            {
                await jsonBackupStream.WriteLineAsync(",");
            }
            
            var json = await WordDefinitionHelper.ToJson(tuple.wordDefinition);
            await jsonBackupStream.WriteAsync(json);
            
            firstWrite = false;

            if (counter++ % 1000 == 0)
            {
                await jsonBackupStream.FlushAsync();
            }
        }
        await jsonBackupStream.WriteLineAsync();
        await jsonBackupStream.WriteLineAsync("]");
    }
    
    public async ValueTask DisposeAsync()
    {
        wordDefinitionChannel.Writer.Complete();
        await consumerTask;
        await jsonBackupStream.FlushAsync();
        await jsonBackupStream.DisposeAsync();
        
        GC.SuppressFinalize(this);
    }
}