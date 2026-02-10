using InfoScopeDeveloperToolkit.Core.Abstractions;
using InfoScopeDeveloperToolkit.Core.Services;

namespace InfoScopeDeveloperToolkit.Tools.Sample.Tools;

public sealed class ErrorThreadExtractTool : ITool
{
    public string Id => "error-thread-kivonat";
    public string Name => "Error thread kivonat";
    public string Description => "Nagy log bemenetből hibás thread kivonatot készít.";

    public ToolParameterDefinition[] ParameterDefinitions =>
    [
        new("inputLog", "Input log", "A teljes log szövege (több soros).", true),
        new("exportPath", "Export útvonal (opcionális)", "Ha megadod, a kivonat TXT fájlba is mentésre kerül.", false)
    ];

    public async Task RunAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        var inputLog = context.Parameters["inputLog"];
        context.Progress.Report(new ToolProgressUpdate(20, "Input log feldolgozása"));

        var summary = await Task.Run(() => ErrorThreadSummaryGenerator.BuildSummary(inputLog), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        context.Log(new ToolLogEvent(DateTimeOffset.Now, LogLevel.Information, summary));

        if (context.Parameters.TryGetValue("exportPath", out var exportPath) && !string.IsNullOrWhiteSpace(exportPath))
        {
            await File.WriteAllTextAsync(exportPath, summary, cancellationToken);
            context.Log(new ToolLogEvent(DateTimeOffset.Now, LogLevel.Information, $"Kivonat exportálva: {exportPath}"));
        }

        context.Progress.Report(new ToolProgressUpdate(100, "Error thread kivonat elkészült"));
    }
}
