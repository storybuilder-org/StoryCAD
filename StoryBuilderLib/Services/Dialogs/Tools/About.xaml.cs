using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;

namespace StoryBuilder.Services.Dialogs.Tools;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class About : Page
{
    public About()
    {
        InitializeComponent();
        string Revision = System.IO.File.ReadAllText(GlobalData.RootDirectory + "\\RevisionID");
        Version.Text = "Version: " + Windows.ApplicationModel.Package.Current.Id.Version.Major + "." + Windows.ApplicationModel.Package.Current.Id.Version.Minor + "." + Windows.ApplicationModel.Package.Current.Id.Version.Build + "." + Revision;
        Path.Text = "Installation Directory: " + GlobalData.RootDirectory;
    }
}
