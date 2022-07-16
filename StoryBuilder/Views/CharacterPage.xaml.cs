using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryBuilder.Controls;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Views;

public sealed partial class CharacterPage : BindablePage
{
    public CharacterViewModel CharVm => Ioc.Default.GetService<CharacterViewModel>();
    public CharacterPage()
    {
        InitializeComponent();
        DataContext = CharVm;
    }
}