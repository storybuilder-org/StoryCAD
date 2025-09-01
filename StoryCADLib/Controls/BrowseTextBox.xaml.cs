using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace StoryCAD.Controls;

public sealed partial class BrowseTextBox : UserControl
{
    public BrowseTextBox()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(
        nameof(Path), typeof(string), typeof(BrowseTextBox), new PropertyMetadata(string.Empty));

    public string Path
    {
        get => (string)GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(string), typeof(BrowseTextBox), new PropertyMetadata(string.Empty));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
        nameof(PlaceholderText), typeof(string), typeof(BrowseTextBox), new PropertyMetadata(string.Empty));

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public static readonly DependencyProperty BrowseModeProperty = DependencyProperty.Register(
        nameof(BrowseMode), typeof(BrowseMode), typeof(BrowseTextBox), new PropertyMetadata(BrowseMode.Folder));

    public BrowseMode BrowseMode
    {
        get => (BrowseMode)GetValue(BrowseModeProperty);
        set => SetValue(BrowseModeProperty, value);
    }

    public static readonly DependencyProperty TextBoxWidthProperty = DependencyProperty.Register(
        nameof(TextBoxWidth), typeof(double), typeof(BrowseTextBox), new PropertyMetadata(300.0));

    public double TextBoxWidth
    {
        get => (double)GetValue(TextBoxWidthProperty);
        set => SetValue(TextBoxWidthProperty, value);
    }

    public static readonly DependencyProperty ButtonTextProperty = DependencyProperty.Register(
        nameof(ButtonText), typeof(string), typeof(BrowseTextBox), new PropertyMetadata("Browse"));

    public string ButtonText
    {
        get => (string)GetValue(ButtonTextProperty);
        set => SetValue(ButtonTextProperty, value);
    }

    public static readonly DependencyProperty ButtonVerticalAlignmentProperty = DependencyProperty.Register(
        nameof(ButtonVerticalAlignment), typeof(VerticalAlignment), typeof(BrowseTextBox), new PropertyMetadata(VerticalAlignment.Center));

    public VerticalAlignment ButtonVerticalAlignment
    {
        get => (VerticalAlignment)GetValue(ButtonVerticalAlignmentProperty);
        set => SetValue(ButtonVerticalAlignmentProperty, value);
    }

    public static readonly DependencyProperty ButtonMarginProperty = DependencyProperty.Register(
        nameof(ButtonMargin), typeof(Thickness), typeof(BrowseTextBox), new PropertyMetadata(new Thickness(0, 25, 10, 0)));

    public Thickness ButtonMargin
    {
        get => (Thickness)GetValue(ButtonMarginProperty);
        set => SetValue(ButtonMarginProperty, value);
    }

    public event EventHandler<string> PathSelected;

    private async void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var windowing = Ioc.Default.GetRequiredService<Windowing>();
        
        if (BrowseMode == BrowseMode.Folder)
        {
            StorageFolder folder = await windowing.ShowFolderPicker();
            if (folder != null)
            {
                Path = folder.Path;
                PathSelected?.Invoke(this, folder.Path);
            }
        }
        else
        {
            FileOpenPicker picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, windowing.WindowHandle);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            
            // Add appropriate file type filters based on use case
            if (BrowseMode == BrowseMode.STBXFile)
            {
                picker.FileTypeFilter.Add(".stbx");
            }
            else
            {
                picker.FileTypeFilter.Add("*");
            }

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Path = file.Path;
                PathSelected?.Invoke(this, file.Path);
            }
        }
    }
}

public enum BrowseMode
{
    Folder,
    File,
    STBXFile
}