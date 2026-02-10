namespace InfoScopeDeveloperToolkit.Core.Services;

public interface IDiagnosticExportService
{
    Task<string> ExportAsync(string outputDirectory, CancellationToken cancellationToken = default);
}
