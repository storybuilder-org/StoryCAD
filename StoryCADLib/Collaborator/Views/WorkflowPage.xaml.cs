using System.Collections.ObjectModel;
using Windows.System;
using Microsoft.UI.Xaml.Input;
using StoryCAD.Collaborator.ViewModels;
using StoryCAD.Services.Collaborator;

namespace StoryCAD.Collaborator.Views;

/// <summary>
///     Updated version of WorkflowPage
/// </summary>
public sealed partial class WorkflowPage : Page
{
    public WorkflowViewModel WorkflowVm = Ioc.Default.GetService<WorkflowViewModel>();

    public WorkflowPage()
    {
        WorkflowVm.Title = "test";
        WorkflowVm.Description = "test description";
        WorkflowVm.ConversationList = new ObservableCollection<string>
        {
            "Assistant says hello",
            "User says howdy"
        };

        InitializeComponent();
        DataContext = WorkflowVm;
    }

    private void SendChat(AutoSuggestBox autoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        throw new NotImplementedException();
    }

    private void InputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && !string.IsNullOrWhiteSpace(InputTextBox.Text))
        {
            SendButton_Click(this, new RoutedEventArgs());
        }
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        Ioc.Default.GetService<CollaboratorService>()!.SendButtonClicked();
        //WorkflowVm.SendButton_Click(sender, e);
    }
}
