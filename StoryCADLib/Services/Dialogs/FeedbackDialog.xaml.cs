using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Services.Dialogs;

public sealed partial class FeedbackDialog : Microsoft.UI.Xaml.Controls.Page
{
	private FeedbackViewModel FeedbackVM = Ioc.Default.GetRequiredService<FeedbackViewModel>();

	public FeedbackDialog()
	{
		InitializeComponent();
	}

	private void ChangeUIText(object sender, SelectionChangedEventArgs e)
	{
		if (FeedbackVM.FeedbackType == 0)
		{
			FeedbackVM.DescriptionTitle = "Issue Description";
			FeedbackVM.DescriptionPlaceholderText = """
			                                        Describe your bug in detail such as :
			                                         - How the bug occurs
			                                         - What you were doing immediately before the bug happens
			                                        """;
			FeedbackVM.ExtraStepsTitle = "Steps to Recreate Issue";
			FeedbackVM.ExtraStepsPlaceholderText = """
			                                       How to recreate the bug in detail, i.e
			                                       1) Open StoryCAD
			                                       2) Open a Story
			                                       3) Click ...
			                                       4) See ... occurs
			                                       """;
		}
		else
		{
			FeedbackVM.DescriptionTitle = "Feature Description";
			FeedbackVM.DescriptionPlaceholderText = "Describe your feature in detail such as what your feature should do.";
			FeedbackVM.ExtraStepsTitle = "How your feature should work";
			FeedbackVM.ExtraStepsPlaceholderText = """
			                                       How exactly your feature should work in detail, i.e
			                                       A new type of node that allows a user to store their research notes with in StoryCAD.
			                                       This should be a simple node with a title and a rich text block that allows a user to
			                                       write down their ideas to their hearts content.
			                                       """;	
		}
	}
}