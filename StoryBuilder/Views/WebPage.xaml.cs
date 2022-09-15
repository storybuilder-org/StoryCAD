using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Views;

public sealed partial class WebPage : Page
{
    WebViewModel WebVM = Ioc.Default.GetRequiredService<WebViewModel>();

    public WebPage()
    {
        this.InitializeComponent();
    }

    private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(WebVM.URL)) { web.Source = new Uri(WebVM.URL); }
        
    }

    private void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        //Prevent crash as URI cast cant be empty.
        try
        {
            if (!string.IsNullOrEmpty(WebVM.URL)) { web.Source = new Uri(WebVM.URL); }

        }
        catch (UriFormatException ex)
        {
            web.Source = new Uri("https://storybuilder.org/");
        }
    }
}