using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

namespace StoryBuilder
{
    public sealed partial class MainWindow : Window
    {
        private MainWindowVM MainWindowVM = Ioc.Default.GetService<MainWindowVM>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void myButton_Click(object sender, RoutedEventArgs e)
        {
            myButton.Content = "Clicked";
        }
    }
}
