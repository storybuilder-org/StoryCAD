using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Microsoft.Web.WebView2.Core;
using StoryBuilder.ViewModels;
using StoryBuilder.Services.Logging;
using LogLevel = StoryBuilder.Services.Logging.LogLevel;

namespace StoryBuilder.Views;

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

    private void Web_OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        WebVM.Query = WebVM.URL.ToString();
        WebVM.Timestamp = DateTime.Now;
        Logger.Log(LogLevel.Info, $"Updated Query to {WebVM.Query} ");
    }

    private void QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        WebVM.SubmitQuery();
    }
}