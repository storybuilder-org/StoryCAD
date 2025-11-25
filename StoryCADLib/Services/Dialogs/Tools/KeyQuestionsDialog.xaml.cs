using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Services.Dialogs.Tools;

public sealed partial class KeyQuestionsDialog : Page
{
    public KeyQuestionsDialog()
    {
        InitializeComponent();
        DataContext = KeyQuestionsVm;
    }

    public KeyQuestionsViewModel KeyQuestionsVm => Ioc.Default.GetService<KeyQuestionsViewModel>();

    public void Next_Click(object o, RoutedEventArgs routedEventArgs)
    {
        KeyQuestionsVm.NextQuestion();
    }

    public void Previous_Click(object o, RoutedEventArgs routedEventArgs)
    {
        KeyQuestionsVm.PreviousQuestion();
    }
}
