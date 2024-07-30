using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System.Reflection;
using StoryCAD.Services.Navigation;
//using StoryCollaborator.Models;
using StoryCAD.Collaborator.Views;
using StoryCAD.Services.Collaborator;
using Microsoft.UI.Xaml.Controls;

namespace StoryCAD.Collaborator.ViewModels;

/// <summary>
/// VM for the main window (WizardShell) of the application
/// </summary>
public class WizardViewModel : ObservableRecipient
{
    public LogService logger = Ioc.Default.GetService<LogService>();
    public CollaboratorService collaborator = Ioc.Default.GetService<CollaboratorService>();

    #region public properties
    public string Title { get; set; }
    public string Description { get; set; }
    public ObservableCollection<NavigationViewItem> MenuSteps
    { get; set; }

    private object _selectedItem;
    public object SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    private StoryElement _model;
    public StoryElement Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }
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

    private Visibility _acceptVisibility = Visibility.Visible;
    public Visibility AcceptVisibility
    {
        get => _acceptVisibility;
        set => SetProperty(ref _acceptVisibility, value);
    }

    private Visibility _exitVisibility = Visibility.Visible;
    public Visibility ExitVisibility
    {
        get => _exitVisibility;
        set => SetProperty(ref _exitVisibility, value);
    }


    #endregion

    #region Commands
    public RelayCommand AcceptCommand { get; }

    public RelayCommand ExitCommand { get; }

    // public RelayCommand HelpCommand { get; }
    #endregion

    #region Constructor(s)

    public WizardViewModel()
    {
        Title = "Story Collaborator";
        Description = "A tool for creating and collaborating on stories.";
        //Type = StoryItemType 
        MenuSteps = new ObservableCollection<NavigationViewItem>();
        // Configure Collaborator page navigation
        NavigationService nav = Ioc.Default.GetService<NavigationService>();
        try
        {
            nav.Configure("ComboPicker", typeof(ComboPicker));
            nav.Configure("WizardPage", typeof(WorkflowPage));
            nav.Configure("TextAppender", typeof(TextAppender));
            nav.Configure("WelcomePage", typeof(WelcomePage));
        }
        catch (Exception e) { }

        AcceptCommand = new RelayCommand(SaveOutputs);
        ExitCommand = new RelayCommand(ExitCollaborator);

    }

    #endregion // of constructors

    #region Load and Save Model 
    public void LoadModel()
    {
        Ioc.Default.GetService<CollaboratorService>()!.LoadWizardViewModel();
    }

    //   

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
        //var item = args.SelectedItem as NavigationViewItem;
        //if (item is null)
        //    return;
        //CurrentItem = item;
        //CollaboratorService collab = Ioc.Default.GetService<CollaboratorService>();
        //collab.LoadWizardStep(Model, (string)item!.Content);
        ////WizardStepViewModel step = Ioc.Default.GetService<WizardStepViewModel>();
        //step!.LoadModel();
        //collab.ProcessWizardStep();
        //CurrentStep = step.Title;
        NavigationService nav = Ioc.Default.GetService<NavigationService>();
        //// Navigate to the appropriate WizardStep page, passing the WizardStep instance
        //// as the parameter. This invokes the Page's OnNavigatedTo() method, which
        //// Establishes the WizardStepViewModel as the DataContext (for binding)
        //// In turn the ViewModel's Activate() method is invoked.

        nav.NavigateTo(ContentFrame, "WorkflowPage", this);
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
    #endregion // of navigation methods

    #region Command Buttons

    /// <summary>
    /// Process the AcceptCommand button.
    ///
    /// Save any pending OutputProperty values.
    /// </summary>
    private void SaveOutputs()
    {
        Ioc.Default.GetService<CollaboratorService>().SaveOutputs();
        // This command is passed via
    }

    /// <summary>
    /// Process the ExitComand button.
    ///
    /// Resets the NavigationView and hides the Collaborator window.
    /// </summary>
    private void ExitCollaborator()
    {
        // Remove the event handler
        SetCurrentPage(typeof(WelcomePage));
        NavView.SelectionChanged -= NavView_SelectionChanged;
        // Reset the NavigationView.
        MenuSteps.Clear();
        // Optionally, clear FooterMenuItems if used
        // FooterMenuItems.Clear();
        // Reset the selected item
        SelectedItem = null;

        // Reset the current page (without navigation)
        //ContentFrame.Navigate(typeof(WelcomePage));

        collaborator.CollaboratorWindow.AppWindow.Hide();
    }

    public void EnableNavigation()
    {
        NavView.SelectionChanged -= NavView_SelectionChanged;
    }


    #endregion


}
