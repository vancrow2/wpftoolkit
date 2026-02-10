using System.Collections.ObjectModel;
using System.Text;
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
        SelectedTool = Tools.FirstOrDefault(t => t.Id == settings.LastSelectedToolId) ?? Tools.FirstOrDefault();
        LoadParameterEditors(settings);
    }

    partial void OnSelectedToolChanged(ToolItemViewModel? value)
    {
        if (value is null)
        {
            Parameters.Clear();
            return;
        }

        var current = Parameters.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        Parameters.Clear();
        foreach (var definition in value.Descriptor.Parameters)
        {
            Parameters.Add(new ToolParameterViewModel
            {
                Key = definition.Key,
                DisplayName = definition.DisplayName,
                Description = definition.Description,
                Value = current.GetValueOrDefault(definition.Key, string.Empty)
            });
        }
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

    [RelayCommand]
    private async Task RunToolAsync()
    {
        if (SelectedTool is null)
        {
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
            await _toolRunner.RunAsync(SelectedTool.Descriptor.Instance, GetParameterValues(), progress, AppendLog, _cts.Token);
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
        var settings = await _settingsService.LoadAsync();
        settings.LastSelectedToolId = SelectedTool?.Id;
        if (SelectedTool is not null)
        {
            settings.ToolParameters[SelectedTool.Id] = GetParameterValues();
        }

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

    private void LoadParameterEditors(AppSettings settings)
    {
        if (SelectedTool is null)
        {
            return;
        }

        var savedValues = settings.ToolParameters.GetValueOrDefault(SelectedTool.Id)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Parameters.Clear();
        foreach (var definition in SelectedTool.Descriptor.Parameters)
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
