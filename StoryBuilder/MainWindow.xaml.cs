using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;
using WinUIEx;

namespace StoryBuilder;

public sealed partial class MainWindow
{
    private readonly MainWindowVM _mainWindowVm = Ioc.Default.GetService<MainWindowVM>();
    public MainWindow()
    {
        InitializeComponent();
        
    }
}