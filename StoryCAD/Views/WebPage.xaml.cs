using Microsoft.Web.WebView2.Core;
using StoryCADLib.Services;
using StoryCADLib.Services.Logging;

namespace StoryCAD.Views;

public sealed partial class WebPage : Page
{
    private readonly LogService _logger = Ioc.Default.GetRequiredService<LogService>();
    public WebViewModel WebVM = Ioc.Default.GetRequiredService<WebViewModel>();

    public WebPage()
    {
        InitializeComponent();
        DataContext = WebVM;
        WebVM.Refresh = Refresh;
        WebVM.GoForward = GoForward;
        WebVM.GoBack = GoBack;
    }

    public void Refresh() => WebView.Reload();

    public void GoForward() => WebView.GoForward();
    
    public void GoBack() => WebView.GoBack();

    /// <summary>
    ///     This code updates the timestamp and query so that the URL box can be updated.
    /// </summary>
    private void Web_OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        WebVM.Query = WebVM.Url.ToString();
        WebVM.Timestamp = DateTime.Now;
        _logger.Log(LogLevel.Info, $"Updated Query to {WebVM.Query}");
    }

    /// <summary>
    /// This happens when the search icon is clicked.
    /// </summary>
    private void QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        WebVM.SubmitQuery();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}
