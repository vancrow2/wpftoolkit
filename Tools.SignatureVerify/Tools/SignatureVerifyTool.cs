using System.Diagnostics;
using System.Text.Json;
using InfoScopeDeveloperToolkit.Core.Abstractions;
using InfoScopeDeveloperToolkit.Core.Models;

namespace InfoScopeDeveloperToolkit.Tools.SignatureVerify.Tools;

public sealed class SignatureVerifyTool : ITool
{
    private const string AppName = "InfoScope Developer Tool-Kit";

    public string Id => "signature-verify";
    public string Name => "Fájl aláírás ellenőrzése";
    public string Description => "A fájl Authenticode aláírását ellenőrzi külső signtool futtatásával.";

    public ToolParameterDefinition[] ParameterDefinitions =>
    [
        new("filePath", "Fájl útvonala", "Az ellenőrizendő fájl teljes elérési útja.", true)
    ];

    public async Task RunAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        var filePath = context.Parameters["filePath"];
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Az ellenőrizendő fájl nem található.", filePath);
        }

        context.Progress.Report(new ToolProgressUpdate(10, "Signtool elérhetőség ellenőrzése"));

        var configuredPath = await LoadSigntoolPathFromSettingsAsync(cancellationToken);
        var signtoolExecutable = string.IsNullOrWhiteSpace(configuredPath) ? "signtool" : configuredPath;

        if (!string.IsNullOrWhiteSpace(configuredPath) && !File.Exists(configuredPath))
        {
            throw new FileNotFoundException("A beállított SigntoolPath nem található. Ellenőrizd a Settings-ben megadott elérési utat.", configuredPath);
        }

        context.Log(new ToolLogEvent(DateTimeOffset.Now, LogLevel.Information, $"Futtatott parancs: {signtoolExecutable} verify /pa /v \"{filePath}\""));
        context.Progress.Report(new ToolProgressUpdate(35, "Aláírás ellenőrzés fut"));

        var execution = await RunProcessAsync(signtoolExecutable, $"verify /pa /v \"{filePath}\"", cancellationToken);

        context.Log(new ToolLogEvent(DateTimeOffset.Now, LogLevel.Information, $"signtool stdout:{Environment.NewLine}{execution.StdOut}"));
        context.Log(new ToolLogEvent(DateTimeOffset.Now, LogLevel.Information, $"signtool stderr:{Environment.NewLine}{execution.StdErr}"));
        context.Log(new ToolLogEvent(DateTimeOffset.Now, LogLevel.Information, $"signtool exit code: {execution.ExitCode}"));

        context.Progress.Report(new ToolProgressUpdate(90, "Aláírás ellenőrzés befejezése"));

        if (execution.ExitCode != 0)
        {
            throw new InvalidOperationException($"Aláírás ellenőrzés sikertelen (exit code: {execution.ExitCode}).");
        }

        context.Progress.Report(new ToolProgressUpdate(100, "Aláírás ellenőrzés sikeres"));
    }

    private static async Task<string?> LoadSigntoolPathFromSettingsAsync(CancellationToken cancellationToken)
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName,
            "settings.json");

        if (!File.Exists(settingsPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(settingsPath);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, cancellationToken: cancellationToken);
        return settings?.SigntoolPath;
    }

    private static async Task<ProcessExecutionResult> RunProcessAsync(string executable, string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("A signtool folyamat indítása sikertelen.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "A signtool nem érhető el. Telepítsd a Windows SDK-t, vagy állítsd be a SigntoolPath értéket a Settings-ben.",
                ex);
        }

        using var registration = cancellationToken.Register(() =>
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        });

        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return new ProcessExecutionResult(
            process.ExitCode,
            await stdOutTask,
            await stdErrTask);
    }

    private sealed record ProcessExecutionResult(int ExitCode, string StdOut, string StdErr);
}
