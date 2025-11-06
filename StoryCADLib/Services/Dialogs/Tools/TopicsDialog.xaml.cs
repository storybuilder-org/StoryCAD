using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Services.Dialogs.Tools;

public sealed partial class TopicsDialog : Page
{
    public TopicsDialog()
    {
        InitializeComponent();
        DataContext = TopicsVm;
    }

    public TopicsViewModel TopicsVm => Ioc.Default.GetService<TopicsViewModel>();

    public void Previous_Click(object o, RoutedEventArgs routedEventArgs)
    {
        TopicsVm.PreviousSubTopic();
    }
}
