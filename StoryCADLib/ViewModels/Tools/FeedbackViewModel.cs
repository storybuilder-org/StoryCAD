using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Octokit;
using StoryCAD.Services.Json;

namespace StoryCAD.ViewModels.Tools;

public class FeedbackViewModel : ObservableRecipient
{
	#region Variables
	private protected GitHubClient client = new(new ProductHeaderValue("StoryCADFeedbackBot"));

	public FeedbackViewModel()
	{
		Task.Run(async () =>
		{
			Doppler doppler = new();
			Doppler keys = await doppler.FetchSecretsAsync();
			client.Credentials = new(keys.GITHUB_TOKEN, AuthenticationType.Bearer);
		});
	}

	/// <summary>
	/// Title of the Report
	/// </summary>
	public string Title
	{
		get => _title;
		set => SetProperty(ref _title, value);
	}

	private string _title = "";

	/// <summary>
	/// Body of the report
	/// </summary>
	public string Body
	{
		get => _body;
		set => SetProperty(ref _body, value);
	}

	private string _body = "";

	/// <summary>
	/// Type of feedback
	/// Either (0) Bug Report or (1) Feature Request
	/// </summary>
	public int FeedbackType
	{
		get => _FeedbackType;
		set => SetProperty(ref _FeedbackType, value);
	}

	private int _FeedbackType = 0;

	/// <summary>
	/// Title of the Description Text box
	/// </summary>
	public string DescriptionTitle
	{
		get => _descriptionTitle;
		set => SetProperty(ref _descriptionTitle, value);
	}

	private string _extraStepsText = "";

	/// <summary>
	/// Title of the Description Text box
	/// </summary>
	public string ExtraStepsText
	{
		get => _extraStepsText;
		set => SetProperty(ref _extraStepsText, value);
	}

	private string _descriptionTitle = "";

	/// <summary>
	/// placeholder text of the Description Text box
	/// </summary>
	public string DescriptionPlaceholderText
	{
		get => _DescriptionPlaceholderText;
		set => SetProperty(ref _DescriptionPlaceholderText, value);
	}

	private string _DescriptionPlaceholderText = "";

	/// <summary>
	/// Title of the Extra Steps Text box
	/// </summary>
	public string ExtraStepsTitle
	{
		get => _extraStepsTitle;
		set => SetProperty(ref _extraStepsTitle, value);
	}

	private string _extraStepsTitle = "";

	/// <summary>
	/// placeholder text of the Extra Steps Text box
	/// </summary>
	public string ExtraStepsPlaceholderText
	{
		get => _ExtraStepsPlaceholderText;
		set => SetProperty(ref _ExtraStepsPlaceholderText, value);
	}

	private string _ExtraStepsPlaceholderText = "";
	#endregion

	/// <summary>
	/// Creates feedback on Github
	/// </summary>
	public async void CreateFeedback()
	{
		//Append issue type to title
		NewIssue Issue;
		if (FeedbackType == 0)
		{
			Issue = new("[BUG] " + Title);
			Issue.Body = $"""
			              Describe your feature in detail such as what your feature should do:
			              {Body}

			              How your feature should work:
			              {ExtraStepsText}
			              """;
		}
		else
		{
			Issue = new("[Feature Request] " + Title);
			Issue.Labels.Add("enhancement");
			Issue.Body = $"""
			              Describe your feature in detail such as what your feature should do:
			              {Body}
			              
			              How your feature should work:
			              {ExtraStepsText}
			              """;
		}

		
		await client.Issue.Create("storybuilder-org", "StoryCAD", Issue);
	}
}