using CommunityToolkit.Mvvm.ComponentModel;
using Octokit;
using StoryCADLib.Services;
using StoryCADLib.Services.Json;

namespace StoryCADLib.ViewModels.Tools;

public class FeedbackViewModel : ObservableRecipient
{
    /// <summary>
    ///     Creates feedback on GitHub
    /// </summary>
    public async void CreateFeedback()
    {
        try
        {
            //Append issue type to title
            NewIssue Issue;
            if (FeedbackType == 0)
            {
                Issue = new NewIssue("[BUG] " + Title);
                Issue.Body = $"""
                              Describe your feature in detail such as what your feature should do:
                              {Body}

                              How your feature should work:
                              {ExtraStepsText}
                              """;
            }
            else
            {
                Issue = new NewIssue("[Feature Request] " + Title);
                Issue.Labels.Add("enhancement");
                Issue.Body = $"""
                              Describe your feature in detail such as what your feature should do:
                              {Body}

                              How your feature should work:
                              {ExtraStepsText}
                              """;
            }


            Issue.Body += $"\nFeedback ID: {_preferenceService
                .Model.Email.Substring(0, 5)}";

            await client.Issue.Create("storybuilder-org", "StoryCAD", Issue);
        }
        catch (Exception e)
        {
            _logService
                .LogException(LogLevel.Error, e, $"Failed to post feedback due to exception {e.Message}");
        }
    }

    #region Variables

    private protected GitHubClient client = new(new ProductHeaderValue("StoryCADFeedbackBot"));
    private readonly ILogService _logService;
    private readonly PreferenceService _preferenceService;

    public FeedbackViewModel(ILogService logService, PreferenceService preferenceService)
    {
        _logService = logService;
        _preferenceService = preferenceService;
        Task.Run(async () =>
        {
            Doppler doppler = new();
            var keys = await doppler.FetchSecretsAsync();
            client.Credentials = new Credentials(keys.GITHUB_TOKEN, AuthenticationType.Bearer);
        });
    }

    /// <summary>
    ///     Title of the Report
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string _title = "";

    /// <summary>
    ///     Body of the report
    /// </summary>
    public string Body
    {
        get => _body;
        set => SetProperty(ref _body, value);
    }

    private string _body = "";

    /// <summary>
    ///     Type of feedback
    ///     Either (0) Bug Report or (1) Feature Request
    /// </summary>
    public int FeedbackType
    {
        get => _FeedbackType;
        set => SetProperty(ref _FeedbackType, value);
    }

    private int _FeedbackType;

    /// <summary>
    ///     Title of the Description Text box
    /// </summary>
    public string DescriptionTitle
    {
        get => _descriptionTitle;
        set => SetProperty(ref _descriptionTitle, value);
    }

    private string _extraStepsText = "";

    /// <summary>
    ///     Title of the Description Text box
    /// </summary>
    public string ExtraStepsText
    {
        get => _extraStepsText;
        set => SetProperty(ref _extraStepsText, value);
    }

    private string _descriptionTitle = "";

    /// <summary>
    ///     placeholder text of the Description Text box
    /// </summary>
    public string DescriptionPlaceholderText
    {
        get => _DescriptionPlaceholderText;
        set => SetProperty(ref _DescriptionPlaceholderText, value);
    }

    private string _DescriptionPlaceholderText = "";

    /// <summary>
    ///     Title of the Extra Steps Text box
    /// </summary>
    public string ExtraStepsTitle
    {
        get => _extraStepsTitle;
        set => SetProperty(ref _extraStepsTitle, value);
    }

    private string _extraStepsTitle = "";

    /// <summary>
    ///     placeholder text of the Extra Steps Text box
    /// </summary>
    public string ExtraStepsPlaceholderText
    {
        get => _ExtraStepsPlaceholderText;
        set => SetProperty(ref _ExtraStepsPlaceholderText, value);
    }

    private string _ExtraStepsPlaceholderText = "";

    #endregion
}
