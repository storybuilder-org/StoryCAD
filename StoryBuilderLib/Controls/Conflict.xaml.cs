using System.Collections.Generic;
using Microsoft.UI.Xaml;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using Syncfusion.UI.Xaml.Editors;

namespace StoryBuilder.Controls;

public sealed partial class Conflict
{
    public SortedDictionary<string, ConflictCategoryModel> ConflictTypes;
    private string _category;
    private string _subCategory;
    private ConflictCategoryModel _model;
    public string ExampleText { get; set; }

    public Conflict()
    {
        InitializeComponent();
    }
    private void Category_SelectionChanged(object sender, ComboBoxSelectionChangedEventArgs e)
    {
        _category = (string)Category.Items[Category.SelectedIndex];
        _model = ConflictTypes[_category];
        SubCategory.ItemsSource = _model.SubCategories;
        SubCategory.SelectedIndex = -1;
        Example.SelectedIndex = -1;
    }

    private void SubCategory_SelectionChanged(object sender, ComboBoxSelectionChangedEventArgs e)
    {
        if (SubCategory.SelectedIndex > -1)
        {
            _subCategory = (string)SubCategory.Items[SubCategory.SelectedIndex];
            Example.ItemsSource = _model.Examples[_subCategory];
        }
    }

    private void Example_SelectionChanged(object sender, ComboBoxSelectionChangedEventArgs e)
    {
        ExampleText = (string)Example.SelectedItem;
    }

    private void Example_Loaded(object sender, RoutedEventArgs e)
    {
        ConflictTypes = GlobalData.ConflictTypes;
        Category.ItemsSource = ConflictTypes.Keys;
    }
}