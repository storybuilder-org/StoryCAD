using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using System;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Services.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RecentFiles : Page
    {
        public RecentFiles(UnifiedVM vm)
        {
            InitializeComponent();
            UnifiedMenuVM = vm;
            //Loads lastfile1 through to lastfile5 and loads it as long as its not null, empty or blank
            if (!String.IsNullOrWhiteSpace(GlobalData.Preferences.LastFile1)) { Recents.Items.Add(new ListBoxItem { Name = GlobalData.Preferences.LastFile1, Content = Path.GetFileName(GlobalData.Preferences.LastFile1).Replace(".stbx", "") }); }
            if (!String.IsNullOrWhiteSpace(GlobalData.Preferences.LastFile2)) { Recents.Items.Add(new ListBoxItem { Name = GlobalData.Preferences.LastFile2, Content = Path.GetFileName(GlobalData.Preferences.LastFile2).Replace(".stbx", "") }); }
            if (!String.IsNullOrWhiteSpace(GlobalData.Preferences.LastFile3)) { Recents.Items.Add(new ListBoxItem { Name = GlobalData.Preferences.LastFile3, Content = Path.GetFileName(GlobalData.Preferences.LastFile3).Replace(".stbx", "") }); }
            if (!String.IsNullOrWhiteSpace(GlobalData.Preferences.LastFile4)) { Recents.Items.Add(new ListBoxItem { Name = GlobalData.Preferences.LastFile4, Content = Path.GetFileName(GlobalData.Preferences.LastFile4).Replace(".stbx", "") }); }
            if (!String.IsNullOrWhiteSpace(GlobalData.Preferences.LastFile5)) { Recents.Items.Add(new ListBoxItem { Name = GlobalData.Preferences.LastFile5, Content = Path.GetFileName(GlobalData.Preferences.LastFile5).Replace(".stbx", "") }); }


            if (Recents.Items.Count < 1)
            {
                Pannel.Children.Add(new TextBlock() { Text = "No stories have been opened recently", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10) });
            }
        }
        public UnifiedVM UnifiedMenuVM;
    }
}