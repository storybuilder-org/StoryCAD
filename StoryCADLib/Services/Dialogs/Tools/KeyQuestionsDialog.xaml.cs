using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryCAD.ViewModels.Tools;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StoryCAD.Services.Dialogs.Tools;

public sealed partial class KeyQuestionsDialog
{
    public KeyQuestionsViewModel KeyQuestionsVm => Ioc.Default.GetService<KeyQuestionsViewModel>();

    public KeyQuestionsDialog()
    {
        InitializeComponent();
        DataContext = KeyQuestionsVm;
    }

    public void Next_Click(object o, RoutedEventArgs routedEventArgs)
    {
        KeyQuestionsVm.NextQuestion();
    }

    public void Previous_Click(object o, RoutedEventArgs routedEventArgs)
    {
        KeyQuestionsVm.PreviousQuestion();
    }
}