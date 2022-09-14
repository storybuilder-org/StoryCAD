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
}