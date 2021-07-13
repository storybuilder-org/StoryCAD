using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
            // The Page's DataContext is also the control's DataContext
            ProblemViewModel vm = (ProblemViewModel)DataContext;
            ConflictTypes = vm.ConflictTypes;
            Category.ItemsSource = ConflictTypes.Keys;
        }
    }
}
