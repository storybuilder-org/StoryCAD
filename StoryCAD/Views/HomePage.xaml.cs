using ABI.Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;

namespace StoryCAD.Views;

public sealed partial class HomePage
{
	private Windowing WindowingVM = Ioc.Default.GetRequiredService<Windowing>();
    public HomePage() { InitializeComponent(); }
}