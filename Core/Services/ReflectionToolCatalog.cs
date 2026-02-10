using System.Reflection;
using InfoScopeDeveloperToolkit.Core.Abstractions;
using InfoScopeDeveloperToolkit.Core.Models;

namespace InfoScopeDeveloperToolkit.Core.Services;

public sealed class ReflectionToolCatalog(ILogger<ReflectionToolCatalog> logger) : IToolCatalog
{
    public IReadOnlyList<ToolDescriptor> LoadTools(string toolsDirectoryPath)
    {
        var tools = new List<ToolDescriptor>();

        if (!Directory.Exists(toolsDirectoryPath))
        {
            logger.LogWarning("A tool könyvtár nem létezik: {Path}", toolsDirectoryPath);
            return tools;
        }

        foreach (var dllPath in Directory.GetFiles(toolsDirectoryPath, "*.dll", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                var discovered = assembly
                    .GetTypes()
                    .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ITool).IsAssignableFrom(t))
                    .Select(t => Activator.CreateInstance(t) as ITool)
                    .Where(t => t is not null)
                    .Select(t => t!)
                    .Select(t => new ToolDescriptor(t.Id, t.Name, t.Description, t.ParameterDefinitions, t))
                    .ToArray();

                tools.AddRange(discovered);
                logger.LogInformation("{Count} tool betöltve: {Assembly}", discovered.Length, Path.GetFileName(dllPath));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tool assembly betöltési hiba: {DllPath}", dllPath);
            }
        }

        return tools.OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }
}
