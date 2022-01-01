using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

namespace StoryBuilder
{
    public sealed partial class MainWindow
    {
        private readonly MainWindowVM MainWindowVM = Ioc.Default.GetService<MainWindowVM>();
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
