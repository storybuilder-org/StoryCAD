using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.ViewModels.Tools;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StoryCAD.Services.Dialogs.Tools;

public sealed partial class StockScenesDialog : Page
{
    public StockScenesViewModel StockScenesVm => Ioc.Default.GetService<StockScenesViewModel>();

    public StockScenesDialog()
    {
        InitializeComponent();
        DataContext = StockScenesVm;
        StockScenesVm.CategoryName = StockScenesVm.StockSceneCategories[0];
    }
}