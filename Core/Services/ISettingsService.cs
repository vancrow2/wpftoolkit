using InfoScopeDeveloperToolkit.Core.Models;

namespace InfoScopeDeveloperToolkit.Core.Services;

public interface ISettingsService
{
    string SettingsDirectoryPath { get; }
    string SettingsFilePath { get; }
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
