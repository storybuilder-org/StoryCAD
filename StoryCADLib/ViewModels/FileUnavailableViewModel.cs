using System.Diagnostics;

namespace StoryCADLib.ViewModels;

/// <summary>
///     The data of the "File unavailable." dialog that StoryIO.CheckFileAvailability shows for an
///     outline on cloud storage that has not synced. Kept apart from the dialog so that headless
///     code and unit tests can use it (#1638).
/// </summary>
internal class FileUnavailableViewModel
{
    private readonly AppState _appState;

    public FileUnavailableViewModel(AppState appState)
    {
        _appState = appState;
    }

    public string Title => "File unavailable.";

    public string Message => """
                             The story outline you are trying to open is stored on a cloud service, and isn't available currently.
                             Try going online and syncing the file, then try again.

                             Click show help article for more information.
                             """;

    public string PrimaryButtonText => "Show help article";

    public string SecondaryButtonText => "Close";

    public Uri HelpArticleUri => new(new Uri(_appState.ManualBaseUrl),
        "docs/Miscellaneous/Troubleshooting_Cloud_Storage_Providers.html");

    /// <summary>The file that failed the availability check.</summary>
    public string FilePath { get; private set; }

    /// <summary>True after the user selects "Show help article".</summary>
    public bool HelpArticleRequested { get; private set; }

    /// <summary>Clears the answer of an earlier check; the class is a singleton.</summary>
    public void Initialize(string filePath)
    {
        FilePath = filePath;
        HelpArticleRequested = false;
    }

    /// <summary>
    ///     Records the user's answer and opens the help article for "Show help article".
    ///     A headless caller has no user, so it gets no browser.
    /// </summary>
    public void HandleResult(ContentDialogResult result)
    {
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        HelpArticleRequested = true;

        if (!_appState.Headless)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = HelpArticleUri.ToString(),
                UseShellExecute = true
            });
        }
    }
}
