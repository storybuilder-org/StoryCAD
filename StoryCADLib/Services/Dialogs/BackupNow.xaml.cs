namespace StoryCAD.Services.Dialogs;

public sealed partial class BackupNow : Page
{
	public BackupNowVM BackupVM = Ioc.Default.GetRequiredService<BackupNowVM>();
	public BackupNow()
	{
		InitializeComponent();
		string fileName = $"{ShellViewModel.GetModel().ProjectFilename} as of" +
		                  $" {DateTime.Now}".Replace('/', ' ')
			.Replace(':', ' ').Replace(".stbx", "");

		//Set names and paths.
		BackupVM.Name = fileName;
		BackupVM.Location = Ioc.Default.GetRequiredService<PreferenceService>().Model.BackupDirectory;
	}
}