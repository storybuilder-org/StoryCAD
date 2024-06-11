using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Controls;
using StoryCAD.ViewModels;

namespace StoryCAD.Views;

public sealed partial class SettingPage : BindablePage
{
    public SettingViewModel SettingVm => Ioc.Default.GetService<SettingViewModel>();

    public SettingPage()
    {
        InitializeComponent();
        DataContext = SettingVm;
    }
}