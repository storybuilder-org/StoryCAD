using Microsoft.UI.Xaml;

namespace StoryCAD.Services.Dialogs;

public sealed partial class SaveAsDialog : Page
{
    public SaveAsDialog()
    {
        InitializeComponent();
    }

    public SaveAsViewModel SaveAsVm => Ioc.Default.GetService<SaveAsViewModel>();

    private void OnPathSelected(object sender, string path)
    {
        SaveAsVm.ParentFolder = path;
    }
}