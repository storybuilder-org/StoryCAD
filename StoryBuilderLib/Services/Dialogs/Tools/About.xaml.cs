using System.Data.Common;
using CommonServiceLocator;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;

namespace StoryBuilder.Services.Dialogs.Tools;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class About : Page
{
    public About()
    {
        InitializeComponent();
        Version.Text = GlobalData.Version;
        Path.Text = "Installation Directory: " + GlobalData.RootDirectory;
    }
    private void OpenPath(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
            FileName = System.IO.Path.Combine(GlobalData.RootDirectory,"Logs"),
            UseShellExecute = true,
            Verb = "open"
        });;
    }
}
