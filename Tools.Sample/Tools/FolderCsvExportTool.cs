using System.Text;
using InfoScopeDeveloperToolkit.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace InfoScopeDeveloperToolkit.Tools.Sample.Tools;

public sealed class FolderCsvExportTool : ITool
{
    public string Id => "folder-csv-export";
    public string Name => "Mappa tartalom export CSV";
    public string Description => "A mappa fájljait CSV formátumban exportálja.";

    public ToolParameterDefinition[] ParameterDefinitions =>
    [
        new("folderPath", "Mappa elérési út", "A feldolgozandó mappa.", true),
        new("csvPath", "CSV kimenet", "A létrehozandó CSV fájl útvonala.", true)
    ];

    public async Task RunAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        var folderPath = context.Parameters["folderPath"];
        var csvPath = context.Parameters["csvPath"];

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Nem található mappa: {folderPath}");
        }

        var files = Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly);
        var sb = new StringBuilder();
        sb.AppendLine("Nev,MéretBájt,LétrehozvaUtc");

        for (var i = 0; i < files.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fi = new FileInfo(files[i]);
            sb.AppendLine($"\"{fi.Name}\",{fi.Length},{fi.CreationTimeUtc:O}");
            var percentage = files.Length == 0 ? 100 : ((i + 1) / (double)files.Length) * 100;
            context.Progress.Report(new ToolProgressUpdate(percentage, $"Feldolgozva: {fi.Name}"));
        }

        await File.WriteAllTextAsync(csvPath, sb.ToString(), cancellationToken);
        context.Log(new ToolLogEvent(DateTimeOffset.Now, LogLevel.Information, $"CSV export kész: {csvPath}"));
        context.Progress.Report(new ToolProgressUpdate(100, "Kész"));
    }
}
