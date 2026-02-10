using CommunityToolkit.Mvvm.ComponentModel;

namespace InfoScopeDeveloperToolkit.App.ViewModels;

public partial class ToolParameterViewModel : ObservableObject
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }

    [ObservableProperty]
    private string value = string.Empty;
}
