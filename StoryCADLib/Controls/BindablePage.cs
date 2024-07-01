using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Services.Navigation;

namespace StoryCAD.Controls;

public class BindablePage : Page
{
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (DataContext is INavigable navigableViewModel)
            navigableViewModel.Activate(e.Parameter);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (DataContext is INavigable navigableViewModel)
            navigableViewModel.Deactivate(e.Parameter);
    }
}