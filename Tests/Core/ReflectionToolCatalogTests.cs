using InfoScopeDeveloperToolkit.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InfoScopeDeveloperToolkit.Tests.Core;

public class ReflectionToolCatalogTests
{
    [Fact]
    public void LoadTools_BetoltiSampleToolokat()
    {
        var catalog = new ReflectionToolCatalog(new NullLogger<ReflectionToolCatalog>());
        var toolsPath = Path.GetDirectoryName(typeof(InfoScopeDeveloperToolkit.Tools.Sample.Tools.FileHashTool).Assembly.Location)!;

        var tools = catalog.LoadTools(toolsPath);

        Assert.Contains(tools, t => t.Id == "file-sha256");
        Assert.Contains(tools, t => t.Id == "folder-csv-export");
    }
}
