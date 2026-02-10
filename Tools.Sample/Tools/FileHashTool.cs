using System.Security.Cryptography;
using InfoScopeDeveloperToolkit.Core.Abstractions;

namespace InfoScopeDeveloperToolkit.Tools.Sample.Tools;

public sealed class FileHashTool : ITool
{
    public string Id => "file-sha256";
    public string Name => "Fájl SHA-256 hash";
    public string Description => "Kiszámolja egy megadott fájl SHA-256 lenyomatát.";
    public ToolParameterDefinition[] ParameterDefinitions =>
    [
        new("inputPath", "Bemeneti fájl", "A hash-elendő fájl teljes elérési útja.", true),
        new("outputPath", "Kimeneti fájl (opcionális)", "Ha megadod, ide is menti az eredményt.", false)
    ];

    public async Task RunAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        var inputPath = context.Parameters["inputPath"];
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("A bemeneti fájl nem található.", inputPath);
        }

        context.Progress.Report(new ToolProgressUpdate(10, "Fájl olvasása"));
        await using var stream = File.OpenRead(inputPath);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        var hashText = Convert.ToHexString(hash);

        context.Log(new ToolLogEvent(DateTimeOffset.Now, LogLevel.Information, $"SHA-256: {hashText}"));
        context.Progress.Report(new ToolProgressUpdate(90, "Hash számítás elkészült"));

        if (context.Parameters.TryGetValue("outputPath", out var outputPath) && !string.IsNullOrWhiteSpace(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, hashText, cancellationToken);
            context.Log(new ToolLogEvent(DateTimeOffset.Now, LogLevel.Information, $"Eredmény mentve: {outputPath}"));
        }

        context.Progress.Report(new ToolProgressUpdate(100, "Kész"));
    }
}
