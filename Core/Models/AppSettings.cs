namespace InfoScopeDeveloperToolkit.Core.Models;

public sealed class AppSettings
{
    public string? LastSelectedToolId { get; set; }
    public Dictionary<string, Dictionary<string, string>> ToolParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
