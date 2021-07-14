using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using StoryBuilder.DAL;
using StoryBuilder.Models.Tools;
using StoryBuilder.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Controls
{
    public sealed partial class Conflict : UserControl
    {
        public SortedDictionary<string, ConflictCategoryModel> ConflictTypes;
        private string category;
        private string subCategory;
        private ConflictCategoryModel model;
        private string example;

        public Conflict()
        {
            this.InitializeComponent();
        }

        private void Category_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            category = (string) Category.Items[Category.SelectedIndex];
            model = ConflictTypes[category];
            SubCategory.ItemsSource = model.SubCategories;
            SubCategory.SelectedIndex = -1;
            Example.SelectedIndex = -1;
        }

        private void SubCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SubCategory.SelectedIndex > -1)
            {
                subCategory = (string)SubCategory.Items[SubCategory.SelectedIndex];
                Example.ItemsSource = model.Examples[subCategory];
            }
        }

        private void Example_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            example = (string) Example.SelectedItem;
        }

        private void Example_Loaded(object sender, RoutedEventArgs e)
        {
            ConflictTypes = StaticData.ConflictTypes;
            Category.ItemsSource = ConflictTypes.Keys;
        }
    }
}
