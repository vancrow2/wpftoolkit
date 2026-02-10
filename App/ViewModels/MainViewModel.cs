using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfoScopeDeveloperToolkit.Core.Abstractions;
using InfoScopeDeveloperToolkit.Core.Models;
using InfoScopeDeveloperToolkit.Core.Services;

namespace InfoScopeDeveloperToolkit.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IToolCatalog _toolCatalog;
    private readonly ISettingsService _settingsService;
    private readonly ToolRunner _toolRunner;
    private readonly IDiagnosticExportService _diagnosticExportService;
    private readonly Dictionary<string, Dictionary<string, string>> _toolParameterCache = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _cts;

    public ObservableCollection<ToolItemViewModel> Tools { get; } = [];
    public ObservableCollection<ToolParameterViewModel> Parameters { get; } = [];

    [ObservableProperty]
    private ToolItemViewModel? selectedTool;

    [ObservableProperty]
    private string statusText = "Készen áll.";

    [ObservableProperty]
    private string logText = string.Empty;

    [ObservableProperty]
    private double progressPercentage;

    [ObservableProperty]
    private bool isRunning;

    [ObservableProperty]
    private string errorThreadInputLog = string.Empty;

    [ObservableProperty]
    private string errorThreadOutputLog = string.Empty;

    public bool IsNotRunning => !IsRunning;
    public string SettingsFilePath => _settingsService.SettingsFilePath;

    public MainViewModel(
        IToolCatalog toolCatalog,
        ISettingsService settingsService,
        ToolRunner toolRunner,
        IDiagnosticExportService diagnosticExportService)
    {
        _toolCatalog = toolCatalog;
        _settingsService = settingsService;
        _toolRunner = toolRunner;
        _diagnosticExportService = diagnosticExportService;
    }

    public async Task InitializeAsync()
    {
        var toolPath = Path.Combine(AppContext.BaseDirectory, "tools");
        var tools = _toolCatalog.LoadTools(toolPath)
            .Select(d => new ToolItemViewModel(d));

        foreach (var item in tools)
        {
            Tools.Add(item);
        }

        var settings = await _settingsService.LoadAsync();
        foreach (var entry in settings.ToolParameters)
        {
            _toolParameterCache[entry.Key] = new Dictionary<string, string>(entry.Value, StringComparer.OrdinalIgnoreCase);
        }

        SelectedTool = Tools.FirstOrDefault(t => t.Id == settings.LastSelectedToolId) ?? Tools.FirstOrDefault();
        LoadParameterEditors(SelectedTool);
    }

    partial void OnSelectedToolChanged(ToolItemViewModel? value)
    {
        PersistCurrentParameters();
        LoadParameterEditors(value);
    }

    partial void OnIsRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotRunning));
    }

    private void AppendLog(ToolLogEvent evt)
    {
        var line = $"[{evt.Timestamp:HH:mm:ss}] {evt.Level}: {evt.Message}";
        LogText = string.IsNullOrWhiteSpace(LogText) ? line : $"{LogText}{Environment.NewLine}{line}";
    }

    private Dictionary<string, string> GetParameterValues() =>
        Parameters.ToDictionary(p => p.Key, p => p.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

    private void PersistCurrentParameters()
    {
        if (SelectedTool is null)
        {
            return;
        }

        _toolParameterCache[SelectedTool.Id] = GetParameterValues();
    }

    private static string[] ValidateRequiredParameters(ToolItemViewModel selectedTool, IReadOnlyDictionary<string, string> parameters)
    {
        return selectedTool.Descriptor.Parameters
            .Where(d => d.IsRequired && string.IsNullOrWhiteSpace(parameters.GetValueOrDefault(d.Key)))
            .Select(d => d.DisplayName)
            .ToArray();
    }

    [RelayCommand]
    private async Task RunToolAsync()
    {
        if (SelectedTool is null)
        {
            StatusText = "Nincs kiválasztott eszköz.";
            return;
        }

        var parameterValues = GetParameterValues();
        var missingParameters = ValidateRequiredParameters(SelectedTool, parameterValues);
        if (missingParameters.Length > 0)
        {
            StatusText = $"Hiányzó kötelező paraméter(ek): {string.Join(", ", missingParameters)}";
            return;
        }

        _cts = new CancellationTokenSource();
        IsRunning = true;
        ProgressPercentage = 0;
        StatusText = "Futtatás folyamatban...";

        var progress = new Progress<ToolProgressUpdate>(p =>
        {
            ProgressPercentage = p.Percentage;
            StatusText = p.Message;
        });

        try
        {
            PersistCurrentParameters();
            await _toolRunner.RunAsync(SelectedTool.Descriptor.Instance, parameterValues, progress, AppendLog, _cts.Token);
            StatusText = "Futtatás befejezve.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Futtatás megszakítva.";
        }
        catch (Exception ex)
        {
            StatusText = $"Hiba: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelTool()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        PersistCurrentParameters();

        var settings = new AppSettings
        {
            LastSelectedToolId = SelectedTool?.Id,
            ToolParameters = _toolParameterCache.ToDictionary(
                kvp => kvp.Key,
                kvp => new Dictionary<string, string>(kvp.Value, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase)
        };

        await _settingsService.SaveAsync(settings);
        StatusText = "Beállítások mentve.";
    }

    [RelayCommand]
    private void CopyLog()
    {
        Clipboard.SetText(LogText ?? string.Empty);
        StatusText = "Napló másolva a vágólapra.";
    }

    [RelayCommand]
    private async Task ExportDiagnosticPackageAsync()
    {
        var outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "InfoScopeDiagnostics");
        var zipPath = await _diagnosticExportService.ExportAsync(outputDir);
        StatusText = $"Diagnosztikai csomag elkészült: {zipPath}";
    }


    [RelayCommand]
    private async Task ProcessErrorThreadExtractAsync()
    {
        if (string.IsNullOrWhiteSpace(ErrorThreadInputLog))
        {
            StatusText = "Az Input log mező üres.";
            return;
        }

        IsRunning = true;
        ProgressPercentage = 0;
        StatusText = "Error thread kivonat feldolgozása...";

        try
        {
            var output = await Task.Run(() => ErrorThreadSummaryGenerator.BuildSummary(ErrorThreadInputLog));
            ErrorThreadOutputLog = output;
            ProgressPercentage = 100;
            StatusText = "Error thread kivonat elkészült.";
        }
        catch (Exception ex)
        {
            StatusText = $"Hiba a feldolgozás során: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void CopyErrorThreadOutput()
    {
        Clipboard.SetText(ErrorThreadOutputLog ?? string.Empty);
        StatusText = "Kimenet másolva a vágólapra.";
    }

    [RelayCommand]
    private async Task ExportErrorThreadOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(ErrorThreadOutputLog))
        {
            StatusText = "Nincs exportálható kimenet.";
            return;
        }

        var defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var exportPath = Path.Combine(defaultDir, $"error-thread-kivonat-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        await File.WriteAllTextAsync(exportPath, ErrorThreadOutputLog);
        StatusText = $"Kimenet exportálva: {exportPath}";
    }

    private void LoadParameterEditors(ToolItemViewModel? tool)
    {
        Parameters.Clear();
        if (tool is null)
        {
            return;
        }

        var savedValues = _toolParameterCache.GetValueOrDefault(tool.Id)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in tool.Descriptor.Parameters)
        {
            Parameters.Add(new ToolParameterViewModel
            {
                Key = definition.Key,
                DisplayName = definition.DisplayName,
                Description = definition.Description,
                Value = savedValues.GetValueOrDefault(definition.Key, string.Empty)
            });
        }
    }
}
