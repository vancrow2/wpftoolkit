namespace InfoScopeDeveloperToolkit.Core.Abstractions;

public sealed record ToolLogEvent(DateTimeOffset Timestamp, LogLevel Level, string Message);
