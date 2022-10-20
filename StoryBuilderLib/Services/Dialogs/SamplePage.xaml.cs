using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Services.Dialogs;
public sealed partial class SamplePage
{
    private List<string> _paths = new();
    public SamplePage(UnifiedVM vm)
    {
        InitializeComponent();
        foreach (string _SampleStory in Directory.GetFiles(Path.Combine(GlobalData.RootDirectory, "samples")))
        {
            Samples.Items.Add(Path.GetFileName(_SampleStory).Replace(".stbx", ""));
            _paths.Add(_SampleStory);
        }
        UnifiedVM = vm;
    }
    public UnifiedVM UnifiedVM;

    private async void LoadSample(object sender, RoutedEventArgs e)
    {
        if (Samples.SelectedIndex != -1)
        {
            await Ioc.Default.GetRequiredService<ShellViewModel>().OpenFile(_paths[Samples.SelectedIndex]);
            UnifiedVM.Hide();
        }
    }

}