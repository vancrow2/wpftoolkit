using InfoScopeDeveloperToolkit.Core.Models;

namespace InfoScopeDeveloperToolkit.App.ViewModels;

public sealed class ToolItemViewModel(ToolDescriptor descriptor)
{
    public ToolDescriptor Descriptor { get; } = descriptor;
    public string Id => descriptor.Id;
    public string Name => descriptor.Name;
    public string Description => descriptor.Description;
}
