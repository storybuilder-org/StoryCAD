using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryBuilder.ViewModels.Tools;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Services.Dialogs.Tools
{
    // The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238
    public sealed partial class TopicsDialog
    {
        public TopicsViewModel TopicsVm => Ioc.Default.GetService<TopicsViewModel>();

        public void Next_Click(object o, RoutedEventArgs routedEventArgs)
        {
            TopicsVm.NextSubTopic();
        }

    public void Previous_Click(object o, RoutedEventArgs routedEventArgs)
    {
        TopicsVm.PreviousSubTopic();
    }
    public TopicsDialog()
        {
            InitializeComponent();
            DataContext = TopicsVm;
        }
    }
}
