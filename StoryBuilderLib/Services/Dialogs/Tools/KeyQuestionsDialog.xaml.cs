using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryBuilder.ViewModels.Tools;

namespace StoryBuilder.Services.Dialogs.Tools;

public sealed partial class KeyQuestionsDialog
{
    public KeyQuestionsViewModel KeyQuestionsVm => Ioc.Default.GetService<KeyQuestionsViewModel>();

    public KeyQuestionsDialog()
    {
        InitializeComponent();
        DataContext = KeyQuestionsVm;
    }

    public void Next_Click(object o, RoutedEventArgs routedEventArgs) { KeyQuestionsVm.NextQuestion(); }

    public void Previous_Click(object o, RoutedEventArgs routedEventArgs) { KeyQuestionsVm.PreviousQuestion(); }
}