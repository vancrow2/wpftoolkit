using InfoScopeDeveloperToolkit.Core.Models;
using InfoScopeDeveloperToolkit.Core.Services;
using Xunit;

namespace InfoScopeDeveloperToolkit.Tests.Core;

public class JsonSettingsServiceTests
{
    [Fact]
    public async Task SaveAndLoad_Mukodik()
    {
        var appName = $"InfoScopeTest_{Guid.NewGuid():N}";
        var service = new JsonSettingsService(appName);
        var settings = new AppSettings
        {
            LastSelectedToolId = "file-sha256",
            ToolParameters = new Dictionary<string, Dictionary<string, string>>
            {
                ["file-sha256"] = new() { ["inputPath"] = "c:/temp/a.txt" }
            }
        };

        await service.SaveAsync(settings);
        var loaded = await service.LoadAsync();

        Assert.Equal("file-sha256", loaded.LastSelectedToolId);
        Assert.Equal("c:/temp/a.txt", loaded.ToolParameters["file-sha256"]["inputPath"]);

        Directory.Delete(service.SettingsDirectoryPath, recursive: true);
    }
}
