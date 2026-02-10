using InfoScopeDeveloperToolkit.Core.Abstractions;

namespace InfoScopeDeveloperToolkit.Core.Services;

public sealed class ToolRunner(ILogger<ToolRunner> logger)
{
    public async Task RunAsync(
        ITool tool,
        IReadOnlyDictionary<string, string> parameters,
        IProgress<ToolProgressUpdate> progress,
        Action<ToolLogEvent> log,
        CancellationToken cancellationToken)
    {
        try
        {
            log(new ToolLogEvent(DateTimeOffset.Now, LogLevel.Information, $"A(z) '{tool.Name}' futtatása elindult."));
            await tool.RunAsync(new ToolExecutionContext
            {
                Parameters = parameters,
                Progress = progress,
                Log = log
            }, cancellationToken);
            log(new ToolLogEvent(DateTimeOffset.Now, LogLevel.Information, "Tool futtatás sikeresen befejeződött."));
        }
        catch (OperationCanceledException)
        {
            log(new ToolLogEvent(DateTimeOffset.Now, LogLevel.Warning, "Tool futtatás megszakítva."));
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tool futtatási hiba");
            log(new ToolLogEvent(DateTimeOffset.Now, LogLevel.Error, ex.Message));
            throw;
        }
    }
}
