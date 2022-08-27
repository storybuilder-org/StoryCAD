using Microsoft.UI.Xaml;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using System.Collections.Generic;
using Syncfusion.UI.Xaml.Editors;

namespace StoryBuilder.Controls;

public sealed partial class Conflict
{
    public SortedDictionary<string, ConflictCategoryModel> ConflictTypes;
    private string category;
    private string subCategory;
    private ConflictCategoryModel model;
    public string ExampleText { get; set; }

    public Conflict()
    {
        InitializeComponent();
    }
    private void Category_SelectionChanged(object sender, ComboBoxSelectionChangedEventArgs e)
    {
        category = (string)Category.Items[Category.SelectedIndex];
        model = ConflictTypes[category];
        SubCategory.ItemsSource = model.SubCategories;
        SubCategory.SelectedIndex = -1;
        Example.SelectedIndex = -1;
    }

    private void SubCategory_SelectionChanged(object sender, ComboBoxSelectionChangedEventArgs e)
    {
        if (SubCategory.SelectedIndex > -1)
        {
            subCategory = (string)SubCategory.Items[SubCategory.SelectedIndex];
            Example.ItemsSource = model.Examples[subCategory];
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