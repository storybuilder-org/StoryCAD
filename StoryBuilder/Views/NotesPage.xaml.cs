using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Views;

public sealed partial class NotesPage : BindablePage
{
    public NotesViewModel NotesVM => Ioc.Default.GetService<NotesViewModel>();

    public NotesPage()
    {
        InitializeComponent();
        DataContext = NotesVM;
    }
}