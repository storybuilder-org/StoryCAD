using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;
using Microsoft.UI.Xaml;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StoryBuilder.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ScenePage : BindablePage
{
    public SceneViewModel SceneVm => Ioc.Default.GetService<SceneViewModel>();

    public ScenePage()
    {
        InitializeComponent();
        DataContext = SceneVm;
    }
}