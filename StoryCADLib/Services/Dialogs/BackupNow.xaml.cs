namespace StoryCAD.Services.Dialogs;

public sealed partial class BackupNow : Page
{
	public BackupNowVM BackupVM = Ioc.Default.GetRequiredService<BackupNowVM>();

    public BackupNow(string outlineName)
	{
		InitializeComponent();

		//Set names and paths.
		BackupVM.Name = $"{outlineName} as of {DateTime.Now:yyyy-MM-dd_HH-mm}";
		BackupVM.Location = Ioc.Default.GetRequiredService<PreferenceService>().Model.BackupDirectory;
	}
}