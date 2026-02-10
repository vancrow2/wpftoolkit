using System.Windows;
using InfoScopeDeveloperToolkit.App.ViewModels;

namespace InfoScopeDeveloperToolkit.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
        Loaded += async (_, _) => await ((MainViewModel)DataContext).InitializeAsync();
    }
}
