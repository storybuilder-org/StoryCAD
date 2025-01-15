using Windows.Storage;
using Microsoft.UI.Xaml;

namespace StoryCAD.Services.Dialogs;

public sealed partial class NewProjectPage : Page
{
	public NewProjectPage(UnifiedVM vm)
	{
		vm.ProjectNameErrorVisibility = Visibility.Collapsed;
		vm.ProjectFolderErrorVisibilty = Visibility.Collapsed;
		InitializeComponent();
		UnifiedVM = vm;
	}

	public UnifiedVM UnifiedVM;

	public bool BrowseButtonClicked { get; set; }
	public bool ProjectFolderExists { get; set; }
	public StorageFolder ParentFolder { get; set; }
	public string ParentFolderPath { get; set; }
	public string ProjectFolderPath { get; set; }

	private async void Browse_Click(object sender, RoutedEventArgs e)
	{
		// Find a home for the new project
		UnifiedVM.ProjectFolderErrorVisibilty = Visibility.Collapsed;
		StorageFolder folder = await Ioc.Default.GetRequiredService<Windowing>().ShowFolderPicker();
		if (folder != null)
		{
			//Test we have write perms
			try
			{
				var file = await folder.CreateFileAsync("StoryCAD" + DateTimeOffset.Now.ToUnixTimeSeconds());
				await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
			}
			catch
			{
				//No perms, force user to pick different folder
				UnifiedVM.ProjectFolderErrorVisibilty = Visibility.Visible;
				UnifiedVM.ProjectPath = "";
				return;
			}

			ParentFolderPath = folder.Path;
			UnifiedVM.ProjectPath = folder.Path;
		}
	}
}