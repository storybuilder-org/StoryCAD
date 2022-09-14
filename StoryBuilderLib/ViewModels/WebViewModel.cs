using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Services.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryBuilder.ViewModels;

public class WebViewModel : ObservableRecipient, INavigable
{

    private string _url = "https://storybuilder.org/";
    public string URL
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    public void Activate(object parameter)
    {
        //throw new NotImplementedException();
    }

    public void Deactivate(object parameter)
    {
        //throw new NotImplementedException();
    }
}