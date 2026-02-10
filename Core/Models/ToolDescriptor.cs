using InfoScopeDeveloperToolkit.Core.Abstractions;

namespace InfoScopeDeveloperToolkit.Core.Models;

public sealed record ToolDescriptor(string Id, string Name, string Description, ToolParameterDefinition[] Parameters, ITool Instance);
