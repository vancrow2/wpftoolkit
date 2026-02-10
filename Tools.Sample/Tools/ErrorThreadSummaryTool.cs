using InfoScopeDeveloperToolkit.Core.Abstractions;

namespace InfoScopeDeveloperToolkit.Tools.Sample.Tools;

public sealed class ErrorThreadSummaryTool : ITool
{
    public string Id => "error-thread-summary";
    public string Name => "Error thread kivonat";
    public string Description => "Nagy log bemenetből hibás thread kivonat készítése dedikált felületen.";
    public ToolParameterDefinition[] ParameterDefinitions => [];

    public Task RunAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Ez az eszköz dedikált UI felületen futtatható.");
    }
}
