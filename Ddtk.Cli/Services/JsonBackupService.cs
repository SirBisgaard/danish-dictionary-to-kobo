using System.Threading.Channels;
using Ddtk.Cli.Helpers;
using Ddtk.Cli.Models;

namespace Ddtk.Cli.Services;

public class JsonBackupService : IAsyncDisposable
{
    private readonly StreamWriter jsonBackupStream;
    
    private readonly Channel<WordDefinition> wordDefinitionChannel;
    private readonly Task consumerTask;
    
    public JsonBackupService(StreamWriter jsonBackupStream)
    {
        this.jsonBackupStream = jsonBackupStream;
        this.jsonBackupStream.AutoFlush = true;
        
        wordDefinitionChannel = Channel.CreateUnbounded<WordDefinition>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = false,
        });

        consumerTask = Task.Run(ConsumeJson);
    }

    public ValueTask AddToQueue(WordDefinition wordDefinition) => wordDefinitionChannel.Writer.WriteAsync(wordDefinition);

    private async Task ConsumeJson()
    {
        var counter = 0L;
        var firstWrite = true;
        await jsonBackupStream.WriteLineAsync("[");
        await foreach (var wordDefinition in wordDefinitionChannel.Reader.ReadAllAsync())
        {
            if (!firstWrite)
            {
                await jsonBackupStream.WriteLineAsync(",");
            }
            
            var json = await WordDefinitionHelper.ToJson(wordDefinition);
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