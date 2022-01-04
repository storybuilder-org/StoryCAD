using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

namespace StoryBuilder;

public sealed partial class MainWindow
{
    private readonly MainWindowVM _mainWindowVm = Ioc.Default.GetService<MainWindowVM>();
    public MainWindow()
    {
        InitializeComponent();
    }
}