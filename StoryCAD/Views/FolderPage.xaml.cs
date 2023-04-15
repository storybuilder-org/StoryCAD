using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.ViewModels;

namespace StoryCAD.Views;

public sealed partial class FolderPage : BindablePage
{
    public FolderViewModel FolderVm => Ioc.Default.GetService<FolderViewModel>();

    public FolderPage()
    {
        InitializeComponent();
        DataContext = FolderVm;
    }
}