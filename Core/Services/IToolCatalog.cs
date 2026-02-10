using InfoScopeDeveloperToolkit.Core.Models;

namespace InfoScopeDeveloperToolkit.Core.Services;

public interface IToolCatalog
{
    IReadOnlyList<ToolDescriptor> LoadTools(string toolsDirectoryPath);
}
