using System.Diagnostics;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.ViewModels.Tools;

namespace StoryBuilder.Services.Dialogs.Tools;

public sealed partial class PreferencesDialog : Page
{
    public PreferencesViewModel PreferencesVm => Ioc.Default.GetService<PreferencesViewModel>();
    public PreferencesDialog()
    {
        InitializeComponent();
        DataContext = PreferencesVm;
        Version.Text = "StoryBuilder Version: " + Windows.ApplicationModel.Package.Current.Id.Version.Major + "." + Windows.ApplicationModel.Package.Current.Id.Version.Minor + "." + Windows.ApplicationModel.Package.Current.Id.Version.Build + "." + Windows.ApplicationModel.Package.Current.Id.Version.Revision;
    }
    private void OpenPath(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = System.IO.Path.Combine(GlobalData.RootDirectory, "Logs"),
            UseShellExecute = true,
            Verb = "open"
        });
    }

    private void OpenDiscordURL(object sender, RoutedEventArgs e)
    {
        Process Browser = new();
        Browser.StartInfo.FileName = @"https://discord.gg/wfZxU4bx6n";
        Browser.StartInfo.UseShellExecute = true;
        Browser.Start();
    }
}