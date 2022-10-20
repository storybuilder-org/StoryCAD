using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels.Tools;

namespace StoryBuilder.Services.Dialogs.Tools;

public sealed partial class StockScenesDialog
{
    public StockScenesViewModel StockScenesVm => Ioc.Default.GetService<StockScenesViewModel>();

    public StockScenesDialog()
    {
        InitializeComponent();
        DataContext = StockScenesVm;
        StockScenesVm.CategoryName = StockScenesVm.StockSceneCategories[0];
    }
}