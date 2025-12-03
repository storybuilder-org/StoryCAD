using StoryCADLib.Models.Tools;

namespace StoryCADLib.Controls;

public sealed partial class Conflict : UserControl
{
    private string _category;
    public SortedDictionary<string, ConflictCategoryModel> ConflictTypes;
    private ConflictCategoryModel _model;
    private string _subCategory;

    public Conflict()
    {
        InitializeComponent();
    }

    public string ExampleText { get; set; }

    private void Category_SelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
    {
        _category = (string)Category.Items[Category.SelectedIndex];
        _model = ConflictTypes[_category];
        SubCategory.ItemsSource = _model.SubCategories;
        SubCategory.SelectedIndex = -1;
        Example.SelectedIndex = -1;
    }

    private void SubCategory_SelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
    {
        if (SubCategory.SelectedIndex > -1)
        {
            _subCategory = (string)SubCategory.Items[SubCategory.SelectedIndex];
            Example.ItemsSource = _model.Examples[_subCategory];
        }
    }

    private void Example_SelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
    {
        ExampleText = (string)Example.SelectedItem;
    }

    private void Example_Loaded(object sender, RoutedEventArgs e)
    {
        ConflictTypes = Ioc.Default.GetRequiredService<ControlData>().ConflictTypes;
        Category.ItemsSource = ConflictTypes.Keys;
    }
}
