using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Microsoft.Web.WebView2.Core;
using StoryBuilder.ViewModels;
using StoryBuilder.Services.Logging;
using LogLevel = StoryBuilder.Services.Logging.LogLevel;

namespace StoryBuilder.Views;

public sealed partial class WebPage : Page
{
    WebViewModel WebVM = Ioc.Default.GetRequiredService<WebViewModel>();
    private LogService Logger = Ioc.Default.GetRequiredService<LogService>();
    
    public WebPage() { InitializeComponent(); }
    
    private void QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        //Prevent crash as URI cast cant be empty.
        try
        {
            if (!string.IsNullOrEmpty(WebVM.Query))
            {
                Logger.Log(LogLevel.Info, $"Checking if {WebVM.Query} is a URI.");
                WebVM.URL = new Uri(WebVM.Query);
                Logger.Log(LogLevel.Info, $"{WebVM.Query} is a valid URI, navigating to it.");
            }

        }
        catch (UriFormatException ex)
        {
            Logger.Log(LogLevel.Info, $"Checking if {WebVM.Query} is not URI, searching it.");
            WebVM.URL = new Uri("https://www.google.com/search?q=" + Uri.EscapeDataString(WebVM.Query));
            Logger.Log(LogLevel.Info, $"URL is: {WebVM.URL}");

        }
    }

    private void Web_OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        WebVM.Query = WebVM.URL.ToString();
        WebVM.Timestamp = DateTime.Now;
        Logger.Log(LogLevel.Info, $"Updated Query to {WebVM.Query} ");
    }

    private void Refresh(object sender, RoutedEventArgs e) { WebView.Reload(); }

    private void GoForward(object sender, RoutedEventArgs e) { WebView.GoForward(); }
    private void GoBack(object sender, RoutedEventArgs e) { WebView.GoBack(); }
}