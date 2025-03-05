﻿using System.Reflection;
using Microsoft.UI.Xaml;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCAD.Services.Dialogs;

public sealed partial class SamplePage : Page
{
    private List<string> paths;
    public SamplePage(UnifiedVM vm)
    {
        InitializeComponent();

        //Gets all samples in CadLib/Assets/Install/samples
        paths = Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(name => name.Contains("StoryCAD.Assets.Install.samples")).ToList();

        foreach (string ManifestName in paths)
        {
            Samples.Items.Add(ManifestName.Split('.')[4].Replace('_',' '));
        }
        UnifiedVM = vm;
    }
    public UnifiedVM UnifiedVM;

    /// <summary>
    /// This opens the sample, selected by the user (Samples.SelectedItem)
    /// </summary>
    private async void LoadSample(object sender, RoutedEventArgs e)
    {
        if (Samples.SelectedIndex != -1)
        {
            //Gets selected sample content
            await using Stream internalResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(paths[Samples.SelectedIndex]);
            using StreamReader reader = new(internalResourceStream);
            string STBXContent = await reader.ReadToEndAsync();

            //Writes file to disk in a temp dir.
            string FilePath = Path.Combine(Path.GetTempPath(), Samples.SelectedItem + ".stbx");
            File.WriteAllText(FilePath, STBXContent);

            //Opens file, closes menu
            await Ioc.Default.GetService<OutlineViewModel>()!.OpenFile(FilePath);
            UnifiedVM.Hide();
        }
    }

}