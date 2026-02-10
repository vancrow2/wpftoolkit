namespace InfoScopeDeveloperToolkit.Core.Abstractions;

public sealed record ToolParameterDefinition(
    string Key,
    string DisplayName,
    string Description,
    bool IsRequired,
    string? Placeholder = null);
