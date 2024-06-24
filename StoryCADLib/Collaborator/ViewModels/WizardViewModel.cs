using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using StoryCAD.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Type = System.Type;
using System.Linq;
using System.Reflection;
using StoryCAD.Services.Navigation;
//using StoryCollaborator.Models;
using StoryCAD.Collaborator.Views;
using StoryCAD.Services.Collaborator;
using StoryCAD.Services.Logging;

namespace StoryCAD.Collaborator.ViewModels;

public class WizardViewModel : ObservableRecipient
{
    public LogService logger = Ioc.Default.GetService<LogService>();

    //VM for the main window (WizardShell) of the application
    // https://learn.microsoft.com/en-us/windows/apps/design/controls/navigationview
    public string Title { get; set; }
    public string Description { get; set; }
    public ObservableCollection<NavigationViewItem> MenuSteps
    { get; set; }

    private StoryElement _model;
    public StoryElement Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    /// <summary>
    /// All of the StoryElement model's properties are 
    /// stored in a dictionary for easy access. They dictionary's
    /// keyed by propertyname and returns the property's PropertyInfo
    /// value. The PropertyInfo object's GetValue() and SetValue() 
    /// methods can then be used to update the model.
    /// LoadProperties() initializes ModelProperties for a given model.
    /// </summary>
    public SortedDictionary<string, PropertyInfo> ModelProperties { get; set; }
    public StoryItemType ItemType { get; set; }
    public Frame ContentFrame { get; set; }
    public NavigationView NavView { get; set; }

    private NavigationViewItem _currentItem;
    public NavigationViewItem CurrentItem
    {
        get => _currentItem;
        set => SetProperty(ref _currentItem, value);
    }

    private string _currentStep;
    public string CurrentStep
    {
        get => _currentStep;
        set => SetProperty(ref _currentStep, value);
    }

    #region public methods

    #region Load and Save Model 
    public void LoadModel()
    {
        Ioc.Default.GetService<CollaboratorService>().LoadWizardViewModel();
    }
    public void SaveModel() { }

    #endregion

    #region Navigation methods    
    public void NavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        // Navigates, but does not update the Menu.
        // ContentFrame.Navigate(typeof(HomePage));

        SetCurrentNavigationViewItem(GetNavigationViewItems(typeof(WelcomePage)).First());
    }

    public void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        SetCurrentNavigationViewItem(args.SelectedItemContainer as NavigationViewItem);
    }

    /// <summary>
    /// Process the selection  of the WizardShell NavigationView
    /// SelectionChanged event. This is triggered when the user clicks on
    /// a NavigationView Menu Item on the WizardShell page, activating
    /// the loading, display and processing of a WizardStepModel.
    /// 
    /// Each WizardStepModel corresponds to a property on a StoryElement,
    /// a 'story property', and will use other property's input values, examples,
    /// and a prompt to produce an OpenAI completion to recommend a value
    /// for the desired output property.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var item = args.SelectedItem as NavigationViewItem;
        CurrentItem = item;
        CollaboratorService collab = Ioc.Default.GetService<CollaboratorService>();
        collab.LoadWizardStep(Model, (string)item!.Content);
        WizardStepViewModel step = Ioc.Default.GetService<WizardStepViewModel>();
        step!.LoadModel();
        collab.ProcessWizardStep();
        CurrentStep = step.Title;
        NavigationService nav = Ioc.Default.GetService<NavigationService>();
        // Navigate to the appropriate WizardStep page, passing the WizardStep instance
        // as the parameter. This invokes the Page's OnNavigatedTo() method, which
        // Establishes the WizardStepViewModel as the DataContext (for binding)
        // In turn the ViewModel's Activate() method is invoked.

        nav.NavigateTo(ContentFrame, step.PageType, step);
    }

    public List<NavigationViewItem> GetNavigationViewItems()
    {
        var result = new List<NavigationViewItem>();
        var items = NavView.MenuItems.Select(i => (NavigationViewItem)i).ToList();
        items.AddRange(NavView.FooterMenuItems.Select(i => (NavigationViewItem)i));
        result.AddRange(items);

        foreach (NavigationViewItem mainItem in items)
        {
            result.AddRange(mainItem.MenuItems.Select(i => (NavigationViewItem)i));
        }

        return result;
    }

    public List<NavigationViewItem> GetNavigationViewItems(Type type)
    {
        return GetNavigationViewItems().Where(i => i.Tag.ToString() == type.FullName).ToList();
    }

    public List<NavigationViewItem> GetNavigationViewItems(Type type, string title)
    {
        return GetNavigationViewItems(type).Where(ni => ni.Content.ToString() == title).ToList();
    }

    public void SetCurrentNavigationViewItem(NavigationViewItem item)
    {
        if (item == null)
        {
            return;
        }

        if (item.Tag == null)
        {
            return;
        }

        ContentFrame.Navigate(Type.GetType(item.Tag.ToString()), item.Content);
        NavView.Header = item.Content;
        NavView.SelectedItem = item;
    }

    public NavigationViewItem GetCurrentNavigationViewItem()
    {
        return NavView.SelectedItem as NavigationViewItem;
    }

    public void SetCurrentPage(Type type)
    {
        ContentFrame.Navigate(type);
    }
    public void SetCurrentPage(string typeName)
    {
        Type type = Type.GetType("StoryCAD.Collaborator.Views." + typeName);
        ContentFrame.Navigate(type);
    }

    public void OnNavigatedTo(object parameter)
    {

        // TODO: Replace with real data.
        //var data = await _sampleDataService.GetListDetailsDataAsync();

        //foreach (var item in data)
        //{
        //    Steps.Add(item);
        //}
    }

    public void OnNavigatedFrom()
    {
    }

    //public void EnsureItemSelected()
    //{
    //    if (Selected == null)
    //    {
    //        Selected = SampleItems.First();
    //    }
    //}

    #endregion

    #region CommandBar Relay Commands

    public RelayCommand HelpCommand { get; }

    #endregion

    #region Constructor(s)

    public WizardViewModel()
    {
        Title = "Story Collaborator";
        Description = "A tool for creating and collaborating on stories.";
        //ItemType = StoryItemType 
        MenuSteps = new ObservableCollection<NavigationViewItem>();
        ModelProperties = new SortedDictionary<string, PropertyInfo>();
        // Configure Collaborator page navigation
        NavigationService nav = Ioc.Default.GetService<NavigationService>();
        try
        {
            nav.Configure("ComboPicker", typeof(ComboPicker));
            nav.Configure("ResponsePicker", typeof(ResponsePicker));
            nav.Configure("TextAppender", typeof(TextAppender));
            nav.Configure("WelcomePage", typeof(WelcomePage));
        }
        catch (Exception e) { }
    }

    #endregion

    #endregion  // of public methods
}
