using Microsoft.UI.Xaml;

namespace StoryCAD.Services.Dialogs;

public sealed partial class HelpPage : Page
{
	public HelpPage() { InitializeComponent(); }

	/// <summary>
	/// Update preferences to hide/show menu
	/// </summary>
	private void Clicked(object sender, RoutedEventArgs e)
	{
		Ioc.Default.GetRequiredService<PreferenceService>().Model.ShowStartupDialog = 
			(bool)(sender as CheckBox).IsChecked;
	}
}