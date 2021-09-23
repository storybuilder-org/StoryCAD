using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Shell
    {
        public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();

        public Shell()
        {
            try
            {
                InitializeComponent();
                this.DataContext = ShellVm;
                Loaded += Shell_Loaded;
            }                         
            catch (Exception ex)
            {
                //TODO: Call exception version of logger
                LogService log = Ioc.Default.GetService<LogService>();
                log.Log(LogLevel.Debug, ex.Message);
                log.Log(LogLevel.Debug, ex.StackTrace);
                //log.Log(LogLevel.Debug, ex.InnerException.Data.Values);
                // Handle exception
            }
            ShellVm.SplitViewFrame = SplitViewFrame;
        }

        private void Shell_Loaded(object sender, RoutedEventArgs e)
        {
            // The Shell_Loaded event is processed in order to obtain and save the XamlRool  
            // and pass it on to ContentDialogs as a WinUI work-around. See
            // https://docs.microsoft.com/en-us/windows/winui/api/microsoft.ui.xaml.controls.contentdialog?view=winui-3.0-preview
            GlobalData.XamlRoot = Content.XamlRoot;
            ShellVm.ShowHomePage();
        }

        /// <summary>
        /// Makes the TreeView lose its selection when there is no corresponding main menu item.
        /// </summary>
        /// <remarks>But I don't know why...</remarks>
        private void SplitViewFrame_OnNavigated(object sender, NavigationEventArgs e)
        {
            NavigationTree.SelectionMode = TreeViewSelectionMode.None;
            NavigationTree.SelectionMode = TreeViewSelectionMode.Single;
        }

        /// <summary>
        /// Navigates to the specified source page type.
        /// </summary>
        public bool Navigate(Type sourcePageType, object parameter = null)
        {
            return SplitViewFrame.Navigate(sourcePageType, parameter);
        }
        private void TreeViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            ShellVm.RightTappedNode = (StoryNodeItem)item.DataContext;
            ShellVm.ShowFlyoutButtons();
        }

        private void TreeViewItem_Invoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            ShellVm.TreeViewNodeClicked(args.InvokedItem);
            args.Handled = true;
        }

        private void AddButton_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            FlyoutShowOptions myOption = new FlyoutShowOptions();
            myOption.ShowMode = FlyoutShowMode.Transient;
            AddStoryElementCommandBarFlyout.ShowAt(NavigationTree, myOption);
        }
    }
}
