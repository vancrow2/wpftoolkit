using System.IO.Compression;

namespace InfoScopeDeveloperToolkit.Core.Services;

public sealed class DiagnosticExportService(
    ISettingsService settingsService,
    string logsDirectoryPath) : IDiagnosticExportService
{
    public async Task<string> ExportAsync(string outputDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var zipPath = Path.Combine(outputDirectory, $"diagnosztika_{timestamp}.zip");

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        if (File.Exists(settingsService.SettingsFilePath))
        {
            archive.CreateEntryFromFile(settingsService.SettingsFilePath, "settings.json");
        }

        if (Directory.Exists(logsDirectoryPath))
        {
            foreach (var logFile in Directory.GetFiles(logsDirectoryPath, "*.log", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                archive.CreateEntryFromFile(logFile, $"logs/{Path.GetFileName(logFile)}");
            }
        }

        await Task.CompletedTask;
        return zipPath;
    }
}
