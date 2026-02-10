namespace InfoScopeDeveloperToolkit.Core.Abstractions;

public sealed class ToolExecutionContext
{
    public required IReadOnlyDictionary<string, string> Parameters { get; init; }
    public required IProgress<ToolProgressUpdate> Progress { get; init; }
    public required Action<ToolLogEvent> Log { get; init; }
}
