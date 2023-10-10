using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Models;
using StoryCAD.ViewModels;

namespace StoryCAD.Services.Dialogs;

public sealed partial class SamplePage : Page
{
    private List<string> paths = new();
    public SamplePage(UnifiedVM vm)
    {
        InitializeComponent();
        foreach (string sampleStory in Directory.GetFiles(Path.Combine(Ioc.Default.GetRequiredService<Developer>().RootDirectory, "samples")))
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