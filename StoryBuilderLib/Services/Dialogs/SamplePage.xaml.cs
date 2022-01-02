using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels;
using System.Collections.Generic;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Services.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SamplePage : Page
    {
        private List<string> paths = new();
        public SamplePage(UnifiedVM vm)
        {
            InitializeComponent();
            foreach (string SampleStory in Directory.GetDirectories(Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, @"Storybuilder\samples")))
            {
                Samples.Items.Add(Path.GetFileName(SampleStory).Replace(".stbx", ""));
                paths.Add(SampleStory);
            }
            UnifiedVM = vm;
        }
        public UnifiedVM UnifiedVM;

        private async void LoadSample(object sender, RoutedEventArgs e)
        {
            await Ioc.Default.GetService<ShellViewModel>().OpenFileFromPath(paths[Samples.SelectedIndex]);
            UnifiedVM.UpdateRecents(paths[Samples.SelectedIndex]);
            UnifiedVM.Hide();
        }
    }
}
