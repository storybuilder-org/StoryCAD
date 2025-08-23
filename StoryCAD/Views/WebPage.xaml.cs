using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using StoryCAD.Models;
using StoryCAD.Services;
using StoryCAD.Services.Logging;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace StoryCAD.Views;

    public sealed partial class WebPage : BindablePage
    {
        WebViewModel WebVM = Ioc.Default.GetRequiredService<WebViewModel>();
        private LogService Logger = Ioc.Default.GetRequiredService<LogService>();

        public WebPage()
        {
            InitializeComponent();
            DataContext = WebVM;
            WebVM.Refresh = Refresh;
            WebVM.GoForward = GoForward;
            WebVM.GoBack = GoBack;
        }

    public void Refresh() { WebView.Reload(); }
    public void GoForward() { WebView.GoForward(); }
    public void GoBack() { WebView.GoBack(); }

    /// <summary>
    /// This code updates the timestamp and query so that the URL box can be updated. 
    /// </summary>
    private void Web_OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        WebVM.Query = WebVM.Url.ToString();
        WebVM.Timestamp = DateTime.Now;
        Logger.Log(LogLevel.Info, $"Updated Query to {WebVM.Query} ");
    }

    /// <summary>
    /// This happens when the search icon is clicked.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) { WebVM.SubmitQuery(); }
    
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}