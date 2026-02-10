using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using InfoScopeDeveloperToolkit.Core.Services;
using InfoScopeDeveloperToolkit.App.ViewModels;
using System.IO;

namespace InfoScopeDeveloperToolkit.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logsPath = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(logsPath, "app-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog(dispose: true));

        services.AddSingleton<ISettingsService>(_ => new JsonSettingsService());
        services.AddSingleton<IToolCatalog, ReflectionToolCatalog>();
        services.AddSingleton<ToolRunner>();
        services.AddSingleton<IErrorThreadSummaryService, ErrorThreadSummaryService>();
        services.AddSingleton<IDiagnosticExportService>(sp =>
            new DiagnosticExportService(sp.GetRequiredService<ISettingsService>(), logsPath));

        services.AddSingleton<MainViewModel>();
        Services = services.BuildServiceProvider();
    }
}
