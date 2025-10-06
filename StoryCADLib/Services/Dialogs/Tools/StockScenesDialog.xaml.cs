using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Services.Dialogs.Tools;

public sealed partial class StockScenesDialog : Page
{
    public StockScenesDialog()
    {
        InitializeComponent();
        DataContext = StockScenesVm;
        StockScenesVm.CategoryName = StockScenesVm.StockSceneCategories[0];
    }

    public StockScenesViewModel StockScenesVm => Ioc.Default.GetService<StockScenesViewModel>();
}
