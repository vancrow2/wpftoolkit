namespace InfoScopeDeveloperToolkit.Core.Abstractions;

public interface ITool
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    ToolParameterDefinition[] ParameterDefinitions { get; }
    Task RunAsync(ToolExecutionContext context, CancellationToken cancellationToken);
}
