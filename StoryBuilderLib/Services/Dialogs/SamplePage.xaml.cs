using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels;
using System.Collections.Generic;
using System.IO;
using StoryBuilder.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Services.Dialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SamplePage : Page
{
    private List<string> paths = new();
    public SamplePage(UnifiedVM vm)
    {
        InitializeComponent();
        foreach (string sampleStory in Directory.GetFiles(Path.Combine(GlobalData.RootDirectory, "samples")))
        {
            Samples.Items.Add(Path.GetFileName(sampleStory).Replace(".stbx", ""));
            paths.Add(sampleStory);
        }
         UnifiedVM = vm;
    }
    public UnifiedVM UnifiedVM;

    private async void LoadSample(object sender, RoutedEventArgs e)
    {
        if (Samples.SelectedIndex != -1)
        {
            await Ioc.Default.GetService<ShellViewModel>().OpenFile(paths[Samples.SelectedIndex]);
            UnifiedVM.Hide();
        }
    }

}