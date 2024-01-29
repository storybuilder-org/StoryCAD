using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Models;

namespace StoryCAD.Collaborator;

public interface IWizardViewModel
{
    // Properties
    string Title { get; set; }
    string Description { get; set; }
    ObservableCollection<IWizardStepViewModel> Steps { get; set; }
    ObservableCollection<NavigationViewItem> MenuSteps { get; set; }
    StoryElement Model { get; set; }
    SortedDictionary<string, PropertyInfo> ModelProperties { get; set; }
    StoryItemType ItemType { get; set; }
    Frame ContentFrame { get; set; }
    NavigationView NavView { get; set; }
    NavigationViewItem CurrentItem { get; set; }
    IWizardStepViewModel CurrentStep { get; set; }

    // Methods
    void LoadModel();
    void SaveModel();
    void LoadProperties();
    void NavigationView_Loaded(object sender, RoutedEventArgs e);
    void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args);
    void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args);
    List<NavigationViewItem> GetNavigationViewItems();
    List<NavigationViewItem> GetNavigationViewItems(Type type);
    List<NavigationViewItem> GetNavigationViewItems(Type type, string title);
    void SetCurrentNavigationViewItem(NavigationViewItem item);
    NavigationViewItem GetCurrentNavigationViewItem();
    void SetCurrentPage(Type type);
    void SetCurrentPage(string typeName);
    void OnNavigatedTo(object parameter);
    void OnNavigatedFrom();
}