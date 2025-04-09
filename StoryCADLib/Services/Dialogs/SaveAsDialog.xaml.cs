using Microsoft.UI.Xaml;

namespace StoryCAD.Services.Dialogs;

public sealed partial class SaveAsDialog : Page
{
    public SaveAsDialog()
    {
        InitializeComponent();
        ProjectPathName.IsReadOnly = true;
    }

    public SaveAsViewModel SaveAsVm => Ioc.Default.GetService<SaveAsViewModel>();

    private async void OnBrowse(object sender, RoutedEventArgs e)
    {
        ProjectPathName.IsReadOnly = false;
        // may throw error if invalid folder location
        SaveAsVm.ParentFolder = (await Ioc.Default.GetService<Windowing>().ShowFolderPicker()).Path;

        if (SaveAsVm.ParentFolder != null)
        {
            ProjectPathName.IsReadOnly = true;
        }
    }
}